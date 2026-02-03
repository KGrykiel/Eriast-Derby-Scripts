using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Entities.Vehicle.VehicleComponents.ComponentTypes;
using Assets.Scripts.Entities.Vehicle;
using Assets.Scripts.Combat.Saves;
using Assets.Scripts.Managers;
using Assets.Scripts.Stages;
using Assets.Scripts.Stages.Lanes;
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
    [HideInInspector] public Stage previousStage; // For smart lane positioning
    [HideInInspector] public int progress = 0;  // INTEGER: D&D-style discrete position
    [HideInInspector] public bool hasLoggedMovementWarningThisTurn = false;
    
    [Header("Lane System")]
    [Tooltip("Current lane this vehicle is in (null if stage has no lanes)")]
    [HideInInspector] public StageLane currentLane;

    [Header("Crew & Seats")]
    [Tooltip("Physical positions where characters sit and control components. " +
             "Each seat references components it can operate and has an assigned character.")]
    public List<VehicleSeat> seats = new();
    
    [Header("Vehicle Components")]
    [Tooltip("Chassis - MANDATORY structural component (stores HP and AC)")]
    public ChassisComponent chassis;
    
    [Tooltip("Power Core - MANDATORY power supply component (stores energy)")]
    public PowerCoreComponent powerCore;
    
    [Tooltip("Optional components (Drive, Weapons, Utilities, etc.)")]
    public List<VehicleComponent> optionalComponents = new();

    // Component coordinator (handles component management)
    private VehicleComponentCoordinator componentCoordinator;
    
    public VehicleStatus Status { get; private set; } = VehicleStatus.Active;

    [HideInInspector]
    public bool hasMovedThisTurn = false;

    void Awake()
    {
        // Initialize component coordinator
        componentCoordinator = new VehicleComponentCoordinator(this);
        componentCoordinator.InitializeComponents();
        
        // Apply size-based modifiers
        ApplySizeModifiers();
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
            if (seat != null)
            {
                seat.ResetTurnState();
            }
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
    /// Get all seats that can currently act (have character and operational components).
    /// </summary>
    public List<VehicleSeat> GetActiveSeats()
    {
        return seats.Where(s => s != null && s.CanAct()).ToList();
    }

    // ==================== CROSS-COMPONENT MODIFIER SYSTEM ====================

    /// <summary>
    /// Apply size-based modifiers to all components.
    /// Called during vehicle initialization.
    /// Size modifiers automatically route to correct components (AC→Chassis, Speed→Drive, etc.)
    /// </summary>
    private void ApplySizeModifiers()
    {
        if (chassis == null)
        {
            Debug.LogWarning($"[Vehicle] {vehicleName} has no chassis - cannot apply size modifiers");
            return;
        }
        
        // Get size modifiers from chassis's size category
        var sizeModifiers = VehicleSizeModifiers.GetModifiers(chassis.sizeCategory, this);
        
        // Apply each modifier to the appropriate component
        foreach (var modifier in sizeModifiers)
        {
            // Route modifier to correct component based on attribute
            VehicleComponent targetComponent = ResolveModifierTarget(modifier.Attribute);
            
            if (targetComponent != null && !targetComponent.isDestroyed)
            {
                targetComponent.AddModifier(modifier);
            }
        }
    }

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
            if (provider.IsOperational)
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
            Attribute.MaxSpeed => optionalComponents.OfType<DriveComponent>().FirstOrDefault(),
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
    /// Route an effect to the appropriate component.
    /// If playerSelectedComponent provided, uses it. Otherwise auto-routes based on effect type.
    /// </summary>
    /// <param name="effect">The effect being applied</param>
    /// <param name="playerSelectedComponent">Component selected by player (null for auto-routing)</param>
    /// <returns>The entity that should receive the effect</returns>
    public Entity RouteEffectTarget(IEffect effect, VehicleComponent playerSelectedComponent = null)
    {
        // Use player selection if provided
        if (playerSelectedComponent != null)
            return playerSelectedComponent;
        
        // Otherwise auto-route based on effect type and attributes
        return RouteEffectByAttribute(effect);
    }
    
    /// <summary>
    /// Route effect to appropriate component by analyzing its attributes.
    /// Used for auto-routing (non-precise targeting).
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
            if (statusEffect.statusEffect != null && statusEffect.statusEffect.modifiers != null && statusEffect.statusEffect.modifiers.Count > 0)
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

    // ==================== VEHICLE STATUS ====================
    
    /// <summary>
    /// Mark vehicle as destroyed. Called immediately when chassis is destroyed.
    /// Emits destruction event via TurnEventBus - subscribers handle the rest.
    /// </summary>
    public void MarkAsDestroyed()
    {
        if (Status == VehicleStatus.Destroyed) return; // Already handled
        
        Status = VehicleStatus.Destroyed;

        // Emit event - TurnStateMachine removes from turn order, GameManager checks game over
        TurnEventBus.EmitVehicleDestroyed(this);
    }
    
    // ==================== OPERATIONAL STATUS ====================

    /// <summary>
    /// Get reason why vehicle cannot operate, or null if operational.
    /// Checks chassis and power core - the minimum requirements for any vehicle operation.
    /// </summary>
    public string GetNonOperationalReason()
    {
        if (chassis == null) return "No chassis installed";
        if (chassis.isDestroyed) return "Chassis destroyed";
        if (powerCore == null) return "No power core installed";
        if (powerCore.isDestroyed) return "Power core destroyed - no power";
        return null;
    }

    /// <summary>
    /// Check if vehicle is operational (has chassis and power core).
    /// </summary>
    public bool IsOperational() => GetNonOperationalReason() == null;

    /// <summary>
    /// Get reason why vehicle cannot move, or null if it can move.
    /// Checks operational status first, then drive system availability and state.
    /// </summary>
    public string GetCannotMoveReason()
    {
        // Check operational status first
        string reason = GetNonOperationalReason();
        if (reason != null) return reason;
        
        // Check drive system
        var driveComponent = GetDriveComponent();
        if (driveComponent == null) return "No drive system installed";
        if (driveComponent.isDestroyed) return "Drive system destroyed";
        if (driveComponent.isManuallyDisabled) return "Drive system manually disabled by engineer";
        if (!driveComponent.CanContributeToMovement()) return "Drive system immobilized by status effect";
        
        return null;
    }

    /// <summary>
    /// Check if vehicle can move (operational + has functional drive system).
    /// </summary>
    public bool CanMove() => GetCannotMoveReason() == null;
    
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
            int currentEnergy = powerCore != null ? powerCore.currentEnergy : 0;
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

