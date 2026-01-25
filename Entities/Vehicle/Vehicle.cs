using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using EventType = Assets.Scripts.Logging.EventType;
using Assets.Scripts.Logging;
using Assets.Scripts.Entities.Vehicle.VehicleComponents.ComponentTypes;
using Assets.Scripts.Entities.Vehicle;
using Assets.Scripts.Core;
using Assets.Scripts.Combat.Saves;
using Assets.Scripts.Managers;
using SkillContext = Assets.Scripts.Skills.Helpers.SkillContext;

/// <summary>
/// Vehicle is a CONTAINER/COORDINATOR for Entity components.
/// Vehicle itself is NOT an Entity - its components (chassis, weapons, etc.) ARE entities.
/// 
/// The vehicle aggregates stats from components and provides convenience properties.
/// Damage to "the vehicle" is actually damage to its chassis component.
/// 
/// MODIFIER SYSTEM:
/// - Components provide cross-component modifiers (e.g., Armor Plating → Chassis +2 AC)
/// - Skills/Stages apply StatusEffects to components (StatusEffects create AttributeModifiers)
/// - All modifiers are stored on individual components, not the vehicle
/// - StatCalculator is the single source of truth for calculating modified values
/// 
/// For targeting:
/// - Target Vehicle → actually targets chassis (the "body" of the vehicle)
/// - Target Component → targets specific component directly
/// </summary>
public class Vehicle : MonoBehaviour
{
    [Header("Vehicle Identity")]
    public string vehicleName;

    public ControlType controlType = ControlType.Player;
    [HideInInspector] public Stage currentStage;
    [HideInInspector] public float progress = 0f;
    [HideInInspector] public bool hasLoggedMovementWarningThisTurn = false;

    private TextMeshProUGUI nameLabel;
    
    [Header("Crew & Seats")]
    [Tooltip("Physical positions where characters sit and control components. " +
             "Each seat references components it can operate and has an assigned character.")]
    public List<VehicleSeat> seats = new List<VehicleSeat>();
    
    [Header("Vehicle Components")]
    [Tooltip("Chassis - MANDATORY structural component (stores HP and AC)")]
    public ChassisComponent chassis;
    
    [Tooltip("Power Core - MANDATORY power supply component (stores energy)")]
    public PowerCoreComponent powerCore;
    
    [Tooltip("Optional components (Drive, Weapons, Utilities, etc.)")]
    public List<VehicleComponent> optionalComponents = new List<VehicleComponent>();

    // Component coordinator (handles component management)
    private VehicleComponentCoordinator componentCoordinator;
    
    public VehicleStatus Status { get; private set; } = VehicleStatus.Active;

    void Awake()
    {
        // Initialize component coordinator
        componentCoordinator = new VehicleComponentCoordinator(this);
        componentCoordinator.InitializeComponents();

        var labelTransform = transform.Find("NameLabel");
        if (labelTransform != null)
        {
            nameLabel = labelTransform.GetComponent<TextMeshProUGUI>();
        }
    }
    
    void OnValidate()
    {
        // Validate component space usage
        if (chassis == null) return;
        
        int netSpace = componentCoordinator?.CalculateNetComponentSpace() ?? 0;
        
        if (netSpace > 0)
        {
            Debug.LogError($"[Vehicle] {vehicleName} exceeds component space by {netSpace} units! " +
                          $"Remove components or upgrade chassis.");
        }
    }
    
    
    // ==================== COMPONENT ACCESS (delegate to coordinator) ====================
    
    public List<VehicleComponent> AllComponents => componentCoordinator?.GetAllComponents() ?? new List<VehicleComponent>();
    
    public bool IsComponentAccessible(VehicleComponent target) => componentCoordinator?.IsComponentAccessible(target) ?? false;
    
    public string GetInaccessibilityReason(VehicleComponent target) => componentCoordinator?.GetInaccessibilityReason(target);
    
    /// <summary>
    /// Get the drive component of this vehicle (if it exists).
    /// </summary>
    public DriveComponent GetDriveComponent() 
        => optionalComponents.OfType<DriveComponent>().FirstOrDefault();

    public void ResetComponentsForNewTurn()
    {
        hasLoggedMovementWarningThisTurn = false;
        
        // Reset seat turn state (seats track action usage now)
        foreach (var seat in seats)
        {
            seat?.ResetTurnState();
        }
    }

    // ==================== SEAT ACCESS ====================
    
    /// <summary>
    /// Get the seat that controls a specific component.
    /// Returns null if component is not controlled by any seat.
    /// </summary>
    public VehicleSeat GetSeatForComponent(VehicleComponent component)
    {
        if (component == null) return null;
        return seats.FirstOrDefault(s => s.controlledComponents.Contains(component));
    }
    
    /// <summary>
    /// Get the seat where a specific character is assigned.
    /// Returns null if character is not assigned to any seat on this vehicle.
    /// </summary>
    public VehicleSeat GetSeatForCharacter(PlayerCharacter character)
    {
        if (character == null) return null;
        return seats.FirstOrDefault(s => s.assignedCharacter == character);
    }
    
    /// <summary>
    /// Get all characters currently crewing this vehicle.
    /// </summary>
    public List<PlayerCharacter> GetCrew()
    {
        return seats
            .Where(s => s?.assignedCharacter != null)
            .Select(s => s.assignedCharacter)
            .ToList();
    }
    
    /// <summary>
    /// Get the character operating a specific component (via their seat).
    /// Returns null if component has no operator.
    /// </summary>
    public PlayerCharacter GetOperatorForComponent(VehicleComponent component)
    {
        var seat = GetSeatForComponent(component);
        return seat?.assignedCharacter;
    }
    
    /// <summary>
    /// Get all seats that can currently act (have character and operational components).
    /// </summary>
    public List<VehicleSeat> GetActiveSeats()
    {
        return seats.Where(s => s != null && s.CanAct()).ToList();
    }

    // ==================== CROSS-COMPONENT MODIFIER SYSTEM ====================

    /// <summary>
    /// Initialize all component-provided modifiers.
    /// Called once during vehicle initialization after all components are discovered.
    /// Components provide modifiers to OTHER components (e.g., Armor Plating → Chassis +2 AC).
    /// For runtime changes (destroy/disable), components handle their own modifier cleanup.
    /// </summary>
    public void InitializeComponentModifiers()
    {
        // Apply modifiers from all active (non-destroyed, non-disabled) providers
        foreach (var provider in AllComponents)
        {
            if (!provider.isDestroyed && !provider.isDisabled)
            {
                provider.ApplyProvidedModifiers(this);
            }
        }
    }
    
    /// <summary>
    /// Update status effects on all components (tick durations, periodic effects, remove expired).
    /// Called at the end of each turn.
    /// </summary>
    public void UpdateStatusEffects()
    {
        foreach (var component in AllComponents)
        {
            component.UpdateStatusEffects();
        }
    }
    
    /// <summary>
    /// Resolve which component should receive a modifier based on the attribute being modified.
    /// Used by cross-component modifiers and effect routing.
    /// </summary>
    public VehicleComponent ResolveModifierTarget(Attribute attribute)
    {
        return attribute switch
        {
            Attribute.MaxHealth => chassis,
            Attribute.ArmorClass => chassis,
            Attribute.MagicResistance => chassis,
            Attribute.Mobility => chassis,
            Attribute.DragCoefficient => chassis,
            Attribute.MaxEnergy => powerCore,
            Attribute.EnergyRegen => powerCore,
            Attribute.Speed => optionalComponents.OfType<DriveComponent>().FirstOrDefault(),
            Attribute.Acceleration => optionalComponents.OfType<DriveComponent>().FirstOrDefault(),
            Attribute.Stability => optionalComponents.OfType<DriveComponent>().FirstOrDefault(),
            Attribute.BaseFriction => optionalComponents.OfType<DriveComponent>().FirstOrDefault(),
            _ => chassis
        };
    }
    
    /// <summary>
    /// Resolve which entity makes a saving throw based on SaveType.
    /// Centralizes save entity resolution - vehicle knows its own component structure.
    /// </summary>
    /// <param name="saveType">Type of save being made</param>
    /// <returns>The entity that makes the save</returns>
    public Entity ResolveSavingEntity(SaveType saveType)
    {
        return saveType switch
        {
            SaveType.Mobility => chassis,  // Chassis has baseMobility
            // Future save types:
            // SaveType.Systems => powerCore,      // PowerCore has system resilience
            // SaveType.Stability => chassis,      // Chassis handles stability
            _ => chassis  // Default to chassis for unknown types
        };
    }
    
    /// <summary>
    /// Route an effect to the appropriate component based on targeting precision and effect type.
    /// </summary>
    /// <param name="effect">The effect being applied</param>
    /// <param name="precision">How precise the skill's targeting is</param>
    /// <param name="playerSelectedComponent">Component selected by player (for Precise targeting)</param>
    /// <returns>The entity that should receive the effect</returns>
    public Entity RouteEffectTarget(IEffect effect, TargetPrecision precision, VehicleComponent playerSelectedComponent = null)
    {
        // Precise targeting: Use exactly what player selected (or fallback to chassis)
        if (precision == TargetPrecision.Precise)
            return playerSelectedComponent != null ? playerSelectedComponent : chassis;
        
        // Vehicle-only targeting: Always chassis (non-precise attacks)
        if (precision == TargetPrecision.VehicleOnly)
            return chassis;
        
        // Auto targeting: Route based on effect type and attributes
        return RouteEffectByAttribute(effect);
    }
    
    /// <summary>
    /// Route effect to appropriate component by analyzing its attributes.
    /// Used for Auto targeting mode.
    /// </summary>
    private Entity RouteEffectByAttribute(IEffect effect)
    {
        if (effect == null)
            return chassis;
        
        // Direct damage always goes to chassis
        if (effect is DamageEffect)
            return chassis;

        // Healing/restoration goes to chassis
        // TODO: Consider energy restoration routing to power core?
        if (effect is ResourceRestorationEffect)
            return chassis;
        
        // Attribute modifiers route by attribute
        if (effect is AttributeModifierEffect modifierEffect)
        {
            VehicleComponent component = ResolveModifierTarget(modifierEffect.attribute);
            return component != null ? component : chassis;
        }
        
        // Status effects route by their first modifier's attribute
        if (effect is ApplyStatusEffect statusEffect)
        {
            if (statusEffect.statusEffect?.modifiers != null && statusEffect.statusEffect.modifiers.Count > 0)
            {
                var firstModifier = statusEffect.statusEffect.modifiers[0];
                VehicleComponent component = ResolveModifierTarget(firstModifier.attribute);
                return component != null ? component : chassis;
            }
            // No modifiers - default to chassis for behavioral effects (stun, etc.)
            return chassis;
        }
        
        // Unknown effect type - default to chassis
        return chassis;
    }

    // ==================== STAGE MANAGEMENT ====================

    void Start()
    {
        if (nameLabel != null)
        {
            nameLabel.text = vehicleName;
        }
        MoveToCurrentStage();
    }

    public void UpdateNameLabel()
    {
        if (nameLabel != null)
        {
            nameLabel.text = vehicleName;
        }
    }

    public void SetCurrentStage(Stage stage)
    {
        if (currentStage != null)
        {
            currentStage.TriggerLeave(this);
        }
        
        currentStage = stage;
        MoveToCurrentStage();
        
        if (currentStage != null)
        {
            currentStage.TriggerEnter(this);
            
            if (stage.isFinishLine)
            {
                RaceHistory.Log(
                    EventType.FinishLine,
                    EventImportance.Critical,
                    $"[FINISH] {vehicleName} crossed the finish line!",
                    stage,
                    this
                );
            }
        }
    }

    private void MoveToCurrentStage()
    {
        if (currentStage != null)
        {
            Vector3 stagePos = currentStage.transform.position;
            transform.position = new Vector3(stagePos.x, stagePos.y, transform.position.z);
        }
    }

    // ==================== VEHICLE DESTRUCTION ====================

    public void DestroyVehicle()
    {
        if (Status == VehicleStatus.Destroyed) return;
        
        VehicleStatus oldStatus = Status;
        Status = VehicleStatus.Destroyed;
        
        RaceHistory.Log(
            EventType.Destruction,
            EventImportance.Critical,
            $"[DEAD] {vehicleName} has been destroyed!",
            currentStage,
            this
        ).WithMetadata("previousStatus", oldStatus.ToString())
         .WithMetadata("finalHealth", chassis?.health ?? 0)
         .WithMetadata("finalStage", currentStage?.stageName ?? "None");
        
        
        bool wasLeading = DetermineIfLeading();
        if (wasLeading)
        {
            RaceHistory.Log(
                EventType.TragicMoment,
                EventImportance.High,
                $"[TRAGIC] {vehicleName} was destroyed while in the lead!",
                currentStage,
                this
            );
        }
        
        // Remove from turn order via GameManager's state machine
        var gameManager = FindFirstObjectByType<GameManager>();
        if (gameManager != null)
        {
            var stateMachine = gameManager.GetStateMachine();
            stateMachine?.RemoveVehicle(this);
        }
    }
    
    private bool DetermineIfLeading()
    {
        return progress > 50f;
    }
    
    // ==================== ENERGY MANAGEMENT ====================
    
    public void RegenerateEnergy()
    {
        if (powerCore == null || powerCore.isDestroyed)
        {
            if (powerCore != null && powerCore.isDestroyed)
            {
                RaceHistory.Log(
                    EventType.Resource,
                    EventImportance.Medium,
                    $"{vehicleName} cannot regenerate energy - Power Core destroyed!",
                    currentStage,
                    this
                ).WithMetadata("powerCoreDestroyed", true)
                 .WithMetadata("currentEnergy", 0);
            }
            return;
        }
        
        powerCore.RegenerateEnergy();
    }

    // ==================== OPERATIONAL STATUS ====================

    public bool IsOperational()
    {
        if (chassis == null || chassis.isDestroyed)
            return false;
        
        if (powerCore == null || powerCore.isDestroyed)
            return false;
        
        if (Status == VehicleStatus.Destroyed)
            return false;
        
        return true;
    }
    
    public string GetNonOperationalReason()
    {
        if (chassis == null)
            return "No chassis installed";
        if (chassis.isDestroyed)
            return "Chassis destroyed";
        
        if (powerCore == null)
            return "No power core installed";
        if (powerCore.isDestroyed)
            return "Power core destroyed - no power";
        
        if (Status == VehicleStatus.Destroyed)
            return "Vehicle destroyed";
        
        return null;
    }

    public bool CanMove()
    {
        if (!IsOperational()) 
            return false;
        
        var driveComponent = optionalComponents.FirstOrDefault(c => c is DriveComponent);
        if (driveComponent == null || driveComponent.isDestroyed || driveComponent.isDisabled)
        {
            return false;
        }
        
        // Check if drive is prevented from moving by status effects (e.g., frozen, immobilized)
        if (!driveComponent.CanContributeToMovement())
        {
            return false;
        }
        
        return true;
    }
    
    public string GetCannotMoveReason()
    {
        string operationalReason = GetNonOperationalReason();
        if (operationalReason != null)
            return operationalReason;
        
        var driveComponent = optionalComponents.FirstOrDefault(c => c is DriveComponent);
        if (driveComponent == null)
            return "No drive system installed";
        if (driveComponent.isDestroyed)
            return "Drive system destroyed";
        if (driveComponent.isDisabled)
            return "Drive system disabled";
        
        // Check for status effects preventing movement
        if (!driveComponent.CanContributeToMovement())
            return "Drive system immobilized by status effect";
        
        return null;
    }
    
    // ==================== TURN & MOVEMENT MANAGEMENT ====================
    
    [HideInInspector]
    public bool hasMovedThisTurn = false;
    
    /// <summary>
    /// Called at the start of this vehicle's turn.
    /// Order: Apply friction (if unpowered) → Regen → Reset power tracking → Pay continuous power → Reset movement flag → Reset seats
    /// </summary>
    public void StartTurn()
    {
        // 0. Apply friction if drive is unpowered/destroyed
        var driveComponent = optionalComponents.OfType<DriveComponent>().FirstOrDefault();
        if (driveComponent != null && (!driveComponent.isPowered || driveComponent.isDestroyed))
        {
            driveComponent.ApplyFriction();
        }
        
        // 1. Regenerate power FIRST (see full resources)
        if (powerCore != null && !powerCore.isDestroyed)
        {
            powerCore.RegenerateEnergy();
        }
        
        // 2. Reset per-turn power tracking
        if (powerCore != null)
        {
            powerCore.ResetTurnPowerTracking();
        }
        
        // 3. PAY for continuous components (drive, shields, sensors)
        //    Drive power cost is based on CURRENT speed (set last turn)
        //    This is paying to RUN THE ENGINE, not to move
        foreach (var component in AllComponents)
        {
            if (component != null && !component.isDestroyed)
            {
                component.DrawTurnPower();  // Automatically skips components with powerDrawPerTurn = 0
            }
        }
        
        // 4. Reset movement flag (player controls when movement happens)
        hasMovedThisTurn = false;
        hasLoggedMovementWarningThisTurn = false;
        
        // 5. Status effects at turn start
        // Note: Status effects are on components, not vehicle directly
        // Components handle their own status effect timing
        
        // 6. Reset seat turn states
        foreach (var seat in seats)
        {
            seat?.ResetTurnState();
        }
    }
    
    /// <summary>
    /// Called at the end of this vehicle's turn.
    /// Order: Force movement if not moved yet → Status effects at turn end
    /// </summary>
    public void EndTurn()
    {
        // 1. FORCE movement if player hasn't triggered it yet
        //    Movement is mandatory (engine is running, vehicle WILL move)
        if (!hasMovedThisTurn)
        {
            RaceHistory.Log(
                EventType.Movement,
                EventImportance.Low,
                $"{vehicleName} automatically moved (player did not trigger movement manually)",
                currentStage,
                this
            ).WithMetadata("automatic", true);
            
            ExecuteMovement();
        }
        
        // 2. Update status effects on all components (tick durations, apply periodic effects)
        UpdateStatusEffects();
    }
    
    /// <summary>
    /// Execute movement for this turn. Can be called manually by player or auto-triggered at end of turn.
    /// Movement is FREE - power was already paid at turn start by drive continuous draw.
    /// Player controls WHEN movement happens (can be between actions).
    /// </summary>
    public bool ExecuteMovement()
    {
        // Already moved this turn
        if (hasMovedThisTurn)
        {
            Debug.LogWarning($"[Vehicle] {vehicleName} has already moved this turn");
            return false;
        }
        
        // Get drive component
        var driveComponent = optionalComponents.OfType<DriveComponent>().FirstOrDefault();
        
        // NO POWER COST HERE - already paid at turn start by drive continuous draw
        float distance = 0f;
        
        if (driveComponent != null)
        {
            // Use current speed (may be 0 if no drive, or decelerating if destroyed)
            distance = driveComponent.currentSpeed;
        }
        
        if (currentStage != null && distance > 0)
        {
            float oldProgress = progress;
            progress += distance;
            
            RaceHistory.Log(
                EventType.Movement,
                EventImportance.Low,
                $"{vehicleName} moved {distance:F1} units (speed {driveComponent?.currentSpeed ?? 0f:F1})",
                currentStage,
                this
            ).WithMetadata("distance", distance)
             .WithMetadata("speed", driveComponent?.currentSpeed ?? 0f)
             .WithMetadata("oldProgress", oldProgress)
             .WithMetadata("newProgress", progress);
            
            // Check if vehicle reached end of stage
            if (progress >= currentStage.length)
            {
                // Advance to next stage (existing logic in TurnController or GameManager)
                // Note: This will be handled by the race management system
            }
        }
        else if (currentStage != null && distance == 0 && driveComponent != null)
        {
            // Log that vehicle couldn't move (either no drive or stopped)
            if (driveComponent.isDestroyed)
            {
                RaceHistory.Log(
                    EventType.Movement,
                    EventImportance.Medium,
                    $"{vehicleName} has stopped (drive destroyed, speed: {driveComponent.currentSpeed:F1})",
                    currentStage,
                    this
                ).WithMetadata("driveDestroyed", true)
                 .WithMetadata("speed", driveComponent.currentSpeed);
            }
        }
        
        hasMovedThisTurn = true;
        return true;
    }
    
    
    // ==================== SPEED HELPERS ====================
    
    /// <summary>
    /// Get the current effective speed of this vehicle.
    /// If no drive component, returns 0.
    /// </summary>
    public float GetCurrentSpeed()
    {
        var drive = optionalComponents.OfType<DriveComponent>().FirstOrDefault();
        return drive?.currentSpeed ?? 0f;
    }
    
    /// <summary>
    /// Get the maximum speed capability of this vehicle (with modifiers).
    /// </summary>
    public float GetMaxSpeed()
    {
        var drive = optionalComponents.OfType<DriveComponent>().FirstOrDefault();
        if (drive == null || drive.isDestroyed)
            return 0f;
        
        return StatCalculator.GatherAttributeValue(
            drive, 
            Attribute.Speed, 
            drive.maxSpeed
        );
    }
    
    // ==================== SKILL EXECUTION ====================
    
    /// <summary>
    /// Execute a skill with resource management.
    /// Context is built by the caller (PlayerController, AI, etc.) who has full knowledge.
    /// Vehicle handles resource validation, consumption, then delegates to SkillExecutor.
    /// </summary>
    /// <param name="ctx">Pre-built skill context with all execution data</param>
    public bool ExecuteSkill(SkillContext ctx)
    {
        Skill skill = ctx.Skill;
        
        if (ctx.TargetEntity == null)
        {
            Debug.LogError($"[Vehicle] ExecuteSkill called with null target!");
            return false;
        }
        
        // Resource validation
        if (!CanAffordSkill(skill))
        {
            int currentEnergy = powerCore?.currentEnergy ?? 0;
            Debug.LogWarning($"[Vehicle] {vehicleName} cannot afford {skill.name} (need {skill.energyCost}, have {currentEnergy})");
            return false;
        }
        
        // Resource consumption
        if (!ConsumeSkillCost(skill, ctx.SourceComponent))
        {
            Debug.LogError($"[Vehicle] {vehicleName} failed to consume resources for {skill.name}");
            return false;
        }
        
        // Delegate to SkillExecutor for resolution
        return Assets.Scripts.Skills.Helpers.SkillExecutor.Execute(ctx);
    }

    /// <summary>
    /// Check if vehicle can afford to use a skill.
    /// </summary>
    private bool CanAffordSkill(Skill skill)
    {
        if (powerCore == null || skill == null) return false;
        return powerCore.CanDrawPower(skill.energyCost, null);
    }
    
    /// <summary>
    /// Consume the energy cost of a skill.
    /// </summary>
    private bool ConsumeSkillCost(Skill skill, VehicleComponent sourceComponent)
    {
        if (powerCore == null || skill == null) return false;
        return powerCore.DrawPower(skill.energyCost, sourceComponent, $"Skill: {skill.name}");
    }
}

