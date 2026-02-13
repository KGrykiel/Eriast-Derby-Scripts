using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Entities.Vehicle.VehicleComponents.ComponentTypes;
using Assets.Scripts.Entities.Vehicle;
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

    // Coordinators (handle distinct concerns)
    private VehicleComponentCoordinator componentCoordinator;
    private VehicleEffectRouter effectRouter;
    
    public VehicleStatus Status { get; private set; } = VehicleStatus.Active;

    [HideInInspector]
    public bool hasMovedThisTurn = false;

    void Awake()
    {
        // Initialize coordinators
        componentCoordinator = new VehicleComponentCoordinator(this);
        effectRouter = new VehicleEffectRouter(this);

        // Initialize components
        componentCoordinator.InitializeComponents();

        // Apply size-based modifiers (after components are initialized)
        componentCoordinator.ApplySizeModifiers(effectRouter);
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

    // ==================== EFFECT ROUTING ====================

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
    /// Route an effect to the appropriate component.
    /// If playerSelectedComponent provided, uses it. Otherwise auto-routes based on effect type.
    /// Delegates to VehicleEffectRouter.
    /// </summary>
    /// <param name="effect">The effect being applied</param>
    /// <param name="playerSelectedComponent">Component selected by player (null for auto-routing)</param>
    /// <returns>The entity that should receive the effect</returns>
    public Entity RouteEffectTarget(IEffect effect, VehicleComponent playerSelectedComponent = null)
        => effectRouter.RouteEffectTarget(effect, playerSelectedComponent);

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

