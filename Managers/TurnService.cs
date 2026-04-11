using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts.Managers;
using Assets.Scripts.Stages;
using Assets.Scripts.Stages.Lanes;
using Assets.Scripts.Conditions;
using Assets.Scripts.Entities.Vehicles;

/// <summary>Stateless utilities for vehicle operations during turns (movement, power, targeting).</summary>
public class TurnService
{
    private readonly List<Vehicle> vehicles;
    
    public IReadOnlyList<Vehicle> AllVehicles => vehicles;

    public TurnService(List<Vehicle> vehicleList)
    {
        vehicles = vehicleList ?? new List<Vehicle>();
    }

    // ==================== POWER MANAGEMENT ====================

    public void DrawContinuousPowerForAllComponents(Vehicle vehicle)
    {
        if (vehicle == null || vehicle.PowerCore == null) return;

        foreach (var component in vehicle.AllComponents)
        {
            if (component == null) continue;

            bool success = component.DrawTurnPower();

            if (!success)
            {
                TurnEventBus.EmitComponentPowerShutdown(
                    vehicle, 
                    component, 
                    component.GetActualPowerDraw(), 
                    vehicle.PowerCore.currentEnergy);
                //TODO: should probably add logic to determine which components get priority power instead of just shutting down everything that can't be powered
                component.SetManuallyDisabled(true);
            }
        }
    }

    // ==================== SPEED/ACCELERATION ====================

    public void AccelerateVehicle(Vehicle vehicle)
    {
        if (vehicle == null) return;
        
        var drive = vehicle.Drive;
        if (drive == null) return;

        if (!drive.IsOperational)
        {
            drive.ApplyFriction();
            return;
        }
        
        drive.AdjustSpeedTowardTarget();
    }
    
    // ==================== MOVEMENT ====================

    /// <summary>Movement is paid for at turn start, and executed during turn or at turn end.</summary>
    public bool ExecuteMovement(Vehicle vehicle)
    {
        if (vehicle == null) return false;

        if (vehicle.HasMovedThisTurn)
        {
            Debug.LogWarning($"[TurnController] {vehicle.vehicleName} has already moved this turn");
            return false;
        }

        if (!vehicle.CanMove())
        {
            if (!vehicle.HasLoggedMovementWarningThisTurn)
            {
                string reason = vehicle.GetCannotMoveReason();
                TurnEventBus.EmitMovementBlocked(vehicle, reason);
                vehicle.MarkMovementWarningLogged();
            }

            vehicle.ApplyMovement(0);
            return false;
        }

        var drive = vehicle.Drive;
        int distance = drive != null ? drive.GetCurrentSpeed() : 0;

        if (distance > 0)
        {
            int oldProgress = vehicle.Progress;
            vehicle.ApplyMovement(distance);
            TurnEventBus.EmitMovementExecuted(vehicle, distance, drive.GetCurrentSpeed(), oldProgress, vehicle.Progress);
            vehicle.NotifyStatusEffectTrigger(RemovalTrigger.OnMovement);
            TryHandleStageTransitions(vehicle);
        }
        else
        {
            vehicle.ApplyMovement(0);
        }

        return true;
    }

    /// <summary>
    /// Checks whether the vehicle has overshot its current stage boundary and transitions it forward.
    /// Loops to handle chained transitions when excess progress carries through multiple stages.
    /// Called after movement and after turn-end as a safety net for non-movement progress changes.
    /// </summary>
    public void TryHandleStageTransitions(Vehicle vehicle)
    {
        if (vehicle == null || vehicle.CurrentStage == null) return;

        while (vehicle.Progress >= vehicle.CurrentStage.length)
        {
            var currentLane = vehicle.CurrentLane;
            Stage nextStage = null;

            if (currentLane != null && currentLane.nextLane != null)
                nextStage = currentLane.nextLane.GetComponentInParent<Stage>();

            if (nextStage != null)
                MoveToStage(vehicle, nextStage, isPlayerChoice: false);
            else
                break;
        }
    }
    
    public void MoveToStage(Vehicle vehicle, Stage stage, bool isPlayerChoice = false)
    {
        if (vehicle == null || stage == null) return;

        Stage previousStage = vehicle.CurrentStage;
        StageLane targetLane = null;
        if (vehicle.CurrentLane != null)
            targetLane = vehicle.CurrentLane.nextLane;

        if (previousStage != null)
            previousStage.TriggerLeave(vehicle);

        vehicle.TransitionToStage(stage);

        stage.TriggerEnter(vehicle, targetLane);

        if (stage.isFinishLine)
            TurnEventBus.EmitFinishLineCrossed(vehicle, stage);

        TurnEventBus.EmitStageEntered(vehicle, stage, previousStage, vehicle.Progress, isPlayerChoice);
    }
    
    // ==================== COMBAT ====================
    
    /// <summary>Returns all active vehicles in the same stage as <paramref name="source"/>, excluding source itself.</summary>
    public List<Vehicle> GetOtherVehiclesInStage(Vehicle source)
    {
        if (source == null || source.CurrentStage == null)
            return new List<Vehicle>();

        var others = new List<Vehicle>();
        foreach (var v in vehicles)
        {
            if (v == source) continue;
            if (v.CurrentStage != source.CurrentStage) continue;
            if (v.Status != VehicleStatus.Active) continue;
            others.Add(v);
        }

        return others;
    }

    /// <summary>Returns all active vehicles in the same stage as <paramref name="source"/>, including source itself.</summary>
    public List<Vehicle> GetAllTargets(Vehicle source)
    {
        if (source == null || source.CurrentStage == null)
            return new List<Vehicle>();

        var targets = new List<Vehicle>();
        foreach (var v in vehicles)
        {
            if (v.CurrentStage != source.CurrentStage) continue;
            if (v.Status != VehicleStatus.Active) continue;
            targets.Add(v);
        }

        return targets;
    }

    /// <summary>Returns all active allies of <paramref name="source"/> in the same stage.</summary>
    public List<Vehicle> GetAlliedTargets(Vehicle source)
    {
        if (source == null || source.team == null || source.CurrentStage == null)
            return new List<Vehicle>();

        var allies = new List<Vehicle>();
        foreach (var v in vehicles)
        {
            if (v == source) continue;
            if (v.team != source.team) continue;
            if (v.CurrentStage != source.CurrentStage) continue;
            if (v.Status != VehicleStatus.Active) continue;
            allies.Add(v);
        }

        return allies;
    }

    /// <summary>
    /// True if both vehicles share the same non-null team.
    /// Independent vehicles (team == null) are never allied with anyone.
    /// </summary>
    public static bool AreAllied(Vehicle a, Vehicle b)
    {
        if (a == null || b == null) return false;
        if (a.team == null || b.team == null) return false;
        return a.team == b.team;
    }

    /// <summary>
    /// True if the two vehicles are not on the same team.
    /// Independent vehicles (team == null) are hostile to everyone, including other independents.
    /// </summary>
    public static bool AreHostile(Vehicle a, Vehicle b)
    {
        if (a == null || b == null) return false;
        if (a == b) return false;
        return !AreAllied(a, b);
    }
}