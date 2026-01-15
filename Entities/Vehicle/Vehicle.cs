using UnityEngine;
using TMPro;
using RacingGame.Events;
using System.Collections.Generic;
using System.Linq;
using EventType = RacingGame.Events.EventType;
using Entities.Vehicle.VehicleComponents;
using Entities.Vehicle.VehicleComponents.ComponentTypes;
using Entities.Vehicle.VehicleComponents.Enums;
using Core;

public enum ControlType
{
    Player,
    AI
}

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
    
    [Header("Vehicle Components")]
    [Tooltip("Chassis - MANDATORY structural component (stores HP and AC)")]
    public ChassisComponent chassis;
    
    [Tooltip("Power Core - MANDATORY power supply component (stores energy)")]
    public PowerCoreComponent powerCore;
    
    [Tooltip("Optional components (Drive, Weapons, Utilities, etc.)")]
    public List<VehicleComponent> optionalComponents = new List<VehicleComponent>();

    // Component coordinator (handles component management)
    private Entities.Vehicle.VehicleComponentCoordinator componentCoordinator;
    
    public VehicleStatus Status { get; private set; } = VehicleStatus.Active;

    void Awake()
    {
        // Initialize component coordinator
        componentCoordinator = new Entities.Vehicle.VehicleComponentCoordinator(this);
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
    
    // ==================== CONVENIENCE PROPERTIES (use StatCalculator directly) ====================
    
    public int health
    {
        get => chassis?.health ?? 0;
        set
        {
            if (chassis == null) return;
            int maxHP = Mathf.RoundToInt(StatCalculator.GatherAttributeValue(chassis, Attribute.MaxHealth, chassis.maxHealth));
            chassis.health = Mathf.Clamp(value, 0, maxHP);
            if (chassis.health <= 0 && !chassis.isDestroyed)
            {
                chassis.isDestroyed = true;
                DestroyVehicle();
            }
        }
    }
    
    public int maxHealth => chassis != null 
        ? Mathf.RoundToInt(StatCalculator.GatherAttributeValue(chassis, Attribute.MaxHealth, chassis.maxHealth)) 
        : 0;
    
    public int energy
    {
        get => powerCore?.currentEnergy ?? 0;
        set
        {
            if (powerCore == null) return;
            int maxE = Mathf.RoundToInt(StatCalculator.GatherAttributeValue(powerCore, Attribute.MaxEnergy, powerCore.maxEnergy));
            powerCore.currentEnergy = Mathf.Clamp(value, 0, maxE);
        }
    }
    
    public int maxEnergy => powerCore != null 
        ? Mathf.RoundToInt(StatCalculator.GatherAttributeValue(powerCore, Attribute.MaxEnergy, powerCore.maxEnergy)) 
        : 0;
    
    public float energyRegen => powerCore != null 
        ? StatCalculator.GatherAttributeValue(powerCore, Attribute.EnergyRegen, powerCore.energyRegen) 
        : 0f;
    
    public float speed
    {
        get
        {
            var drive = optionalComponents.OfType<DriveComponent>().FirstOrDefault();
            if (drive != null && !drive.isDestroyed && !drive.isDisabled)
                return StatCalculator.GatherAttributeValue(drive, Attribute.Speed, drive.maxSpeed);
            return 0f;
        }
    }
    
    public int armorClass => chassis != null 
        ? StatCalculator.GatherDefenseValue(chassis) 
        : 10;
    
    public int GetComponentAC(VehicleComponent targetComponent) 
        => targetComponent != null ? StatCalculator.GatherDefenseValue(targetComponent) : armorClass;

    // ==================== COMPONENT ACCESS (delegate to coordinator) ====================
    
    public List<VehicleComponent> AllComponents => componentCoordinator?.GetAllComponents() ?? new List<VehicleComponent>();
    
    public List<VehicleRole> GetAvailableRoles() => componentCoordinator?.GetAvailableRoles() ?? new List<VehicleRole>();
    
    public bool IsComponentAccessible(VehicleComponent target) => componentCoordinator?.IsComponentAccessible(target) ?? false;
    
    public string GetInaccessibilityReason(VehicleComponent target) => componentCoordinator?.GetInaccessibilityReason(target);
    
    public void ResetComponentsForNewTurn()
    {
        hasLoggedMovementWarningThisTurn = false;
        componentCoordinator?.ResetComponentsForNewTurn();
    }

    // ==================== ENTITY ACCESS (for targeting systems) ====================
    
    public Entity GetPrimaryTarget() => chassis;
    
    public List<Entity> GetAllTargetableEntities()
    {
        return AllComponents
            .Where(c => !c.isDestroyed)
            .Cast<Entity>()
            .ToList();
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
            Attribute.MaxEnergy => powerCore,
            Attribute.EnergyRegen => powerCore,
            Attribute.Speed => optionalComponents.OfType<DriveComponent>().FirstOrDefault(),
            _ => chassis
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
            return playerSelectedComponent ?? chassis;
        
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
        if (effect is ResourceRestorationEffect)
            return chassis;
        
        // Attribute modifiers route by attribute
        if (effect is AttributeModifierEffect modifierEffect)
        {
            VehicleComponent component = ResolveModifierTarget(modifierEffect.attribute);
            return component ?? chassis;
        }
        
        // Status effects route by their first modifier's attribute
        if (effect is ApplyStatusEffect statusEffect)
        {
            if (statusEffect.statusEffect?.modifiers != null && statusEffect.statusEffect.modifiers.Count > 0)
            {
                var firstModifier = statusEffect.statusEffect.modifiers[0];
                VehicleComponent component = ResolveModifierTarget(firstModifier.attribute);
                return component ?? chassis;
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
         .WithMetadata("finalHealth", health)
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
        
        var turnController = FindFirstObjectByType<TurnController>();
        if (turnController != null)
            turnController.RemoveDestroyedVehicle(this);
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
}
