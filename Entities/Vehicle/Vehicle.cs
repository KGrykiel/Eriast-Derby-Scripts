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
    
    public VehicleSeat GetSeatForComponent(VehicleComponent component)
    {
        if (component == null) return null;
        return seats.FirstOrDefault(s => s.controlledComponents.Contains(component));
    }
    
    public List<VehicleSeat> GetActiveSeats()
    {
        return seats.Where(s => s != null && s.CanAct()).ToList();
    }

    // ==================== EFFECT ROUTING ====================

    public void UpdateStatusEffects()
    {
        foreach (var component in AllComponents)
        {
            component.UpdateStatusEffects();
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

