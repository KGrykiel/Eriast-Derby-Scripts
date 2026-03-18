using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Entities.Vehicle.VehicleComponents.ComponentTypes;
using Assets.Scripts.Entities.Vehicle;
using Assets.Scripts.Managers;
using Assets.Scripts.Stages;
using Assets.Scripts.Stages.Lanes;
using Assets.Scripts.StatusEffects;
using RollContext = Assets.Scripts.Combat.Rolls.RollSpecs.RollContext;
using Assets.Scripts.Combat.Rolls.RollSpecs;

/// <summary>
/// Container/coordinator for Entity components. NOT an Entity itself.
/// Damage to "the vehicle" is actually damage to its chassis.
/// Vehicle can be targetted directly, which results in routing the effect to the appropriate component (see RouteEffectTarget).
/// </summary>
public class Vehicle : MonoBehaviour
{
    [Header("Vehicle Identity")]
    public string vehicleName;

    public ControlType controlType = ControlType.Player;

    [HideInInspector] public Stage currentStage;
    [HideInInspector] public Stage previousStage;
    [HideInInspector] public int progress = 0;
    [HideInInspector] public bool hasMovedThisTurn = false;
    [HideInInspector] public bool hasLoggedMovementWarningThisTurn = false;
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

    void Awake()
    {
        componentCoordinator = new VehicleComponentCoordinator(this);
        effectRouter = new VehicleEffectRouter(this);

        componentCoordinator.InitializeComponents();

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
    
    public DriveComponent GetDriveComponent()
        => optionalComponents.OfType<DriveComponent>().FirstOrDefault();

    public VehicleComponent GetComponentOfType(ComponentType type)
        => AllComponents.FirstOrDefault(c => c.componentType == type);

    public void ResetComponentsForNewTurn()
    {
        hasMovedThisTurn = false;
        hasLoggedMovementWarningThisTurn = false;

        // Reset seat turn state (seats track action usage now)
        foreach (var seat in seats)
        {
            seat?.ResetTurnState();
        }
    }

    /// <summary>Applies movement distance to progress and marks the vehicle as having moved.</summary>
    public void ApplyMovement(int distance)
    {
        if (distance > 0 && currentStage != null)
        {
            progress += distance;
        }

        hasMovedThisTurn = true;
    }

    /// <summary>Transitions vehicle to a new stage. Carries over excess progress.</summary>
    public void TransitionToStage(Stage newStage)
    {
        Stage oldStage = currentStage;

        progress -= oldStage != null ? oldStage.length : 0;
        currentStage = newStage;

        Vector3 stagePos = newStage.transform.position;
        transform.position = new Vector3(stagePos.x, stagePos.y, transform.position.z);
    }

    // ==================== SEAT ACCESS ====================
    
    public VehicleSeat GetSeatForComponent(VehicleComponent component)
    {
        if (component == null) return null;
        return seats.FirstOrDefault(s => s.controlledComponents.Contains(component));
    }
    
    public List<VehicleSeat> GetActiveSeats()
    {
        return seats.Where(s => s != null && s.CanAct()).ToList();
    }

    // ==================== STATE QUERIES ====================

    /// <summary>Live vehicle state values that can be queried for threshold checks.</summary>
    public enum RuntimeState
    {
        CurrentSpeed,
        CurrentEnergy,
        CurrentHealth,
        CurrentProgress,
    }

    /// <summary>Returns the current value of a live vehicle state. Used by StateThresholdSpec.</summary>
    public int GetStateValue(RuntimeState state)
    {
        return state switch
        {
            RuntimeState.CurrentSpeed    => GetDriveComponent()?.GetCurrentSpeed() ?? 0,
            RuntimeState.CurrentEnergy   => powerCore?.GetCurrentEnergy() ?? 0,
            RuntimeState.CurrentHealth   => chassis?.GetCurrentHealth() ?? 0,
            RuntimeState.CurrentProgress => progress,
            _                            => 0
        };
    }

    // ==================== EFFECT ROUTING ====================

    public void UpdateStatusEffects()
    {
        foreach (var component in AllComponents)
        {
            component.UpdateStatusEffects();
        }
    }

    /// <summary>Broadcasts a removal trigger to all vehicle components.</summary>
    public void NotifyStatusEffectTrigger(RemovalTrigger trigger)
    {
        foreach (var component in AllComponents)
        {
            component.NotifyStatusEffectTrigger(trigger);
        }
    }

    public Entity RouteEffectTarget(IEffect effect)
        => effectRouter.RouteEffectTarget(effect);

    // ==================== VEHICLE STATUS ====================
    
    public void MarkAsDestroyed()
    {
        if (Status == VehicleStatus.Destroyed) return; // Already handled
        
        Status = VehicleStatus.Destroyed;

        // Emit event - TurnStateMachine removes from turn order, GameManager checks game over
        TurnEventBus.EmitVehicleDestroyed(this);
    }
    
    // ==================== OPERATIONAL STATUS ====================

    /// <summary>Null if operational.</summary>
    public string GetNonOperationalReason()
    {
        if (chassis == null) return "No chassis installed";
        if (chassis.isDestroyed) return "Chassis destroyed";
        if (powerCore == null) return "No power core installed";
        if (powerCore.isDestroyed) return "Power core destroyed - no power";
        return null;
    }

    public bool IsOperational() => GetNonOperationalReason() == null;

    /// <summary>Null if can move.</summary>
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

    public bool CanMove() => GetCannotMoveReason() == null;
    
    // ==================== SKILL EXECUTION ====================
    
    public bool ExecuteSkill(RollContext ctx, Skill skill)
    {
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

        // Skill configuration validation
        if (!Assets.Scripts.Skills.Helpers.SkillValidator.Validate(ctx, skill))
            return false;

        // Execute via RollNodeExecutor
        return RollNodeExecutor.Execute(skill.rollNode, ctx, skill);
    }

    private bool CanAffordSkill(Skill skill)
    {
        if (powerCore == null || skill == null) return false;
        return powerCore.CanDrawPower(skill.energyCost, null);
    }

    private bool ConsumeSkillCost(Skill skill, VehicleComponent sourceComponent)
    {
        if (powerCore == null || skill == null) return false;
        return powerCore.DrawPower(skill.energyCost, sourceComponent, $"Skill: {skill.name}");
    }
}

