using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts.Managers;
using Assets.Scripts.Stages;

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
        if (vehicle == null || vehicle.powerCore == null) return;

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
                    vehicle.powerCore.currentEnergy);
                //TODO: should probably add logic to determine which components get priority power instead of just shutting down everything that can't be powered
                component.SetManuallyDisabled(true);
            }
        }
    }

    // ==================== SPEED/ACCELERATION ====================

    public void AccelerateVehicle(Vehicle vehicle)
    {
        if (vehicle == null) return;
        
        var drive = vehicle.GetDriveComponent();
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
        
        if (vehicle.hasMovedThisTurn)
        {
            Debug.LogWarning($"[TurnController] {vehicle.vehicleName} has already moved this turn");
            return false;
        }
        
        if (!vehicle.CanMove())
        {
            if (!vehicle.hasLoggedMovementWarningThisTurn)
            {
                string reason = vehicle.GetCannotMoveReason();
                TurnEventBus.EmitMovementBlocked(vehicle, reason);
                vehicle.hasLoggedMovementWarningThisTurn = true;
            }
            
            vehicle.hasMovedThisTurn = true;
            return false;
        }
        
        var drive = vehicle.GetDriveComponent();
        int distance = drive != null ? drive.GetCurrentSpeed() : 0;
        
        if (vehicle.currentStage != null && distance > 0)
        {
            int oldProgress = vehicle.progress;
            vehicle.progress += distance;
            TurnEventBus.EmitMovementExecuted(vehicle, distance, drive.GetCurrentSpeed(), oldProgress, vehicle.progress);
        }
        
        vehicle.hasMovedThisTurn = true;
        return true;
    }
    
    public void MoveToStage(Vehicle vehicle, Stage stage, bool isPlayerChoice = false)
    {
        if (vehicle == null || stage == null) return;

        Stage previousStage = vehicle.currentStage;

        if (previousStage != null)
            previousStage.TriggerLeave(vehicle);

        vehicle.progress -= previousStage != null ? previousStage.length : 0;
        vehicle.currentStage = stage;

        Vector3 stagePos = stage.transform.position;
        vehicle.transform.position = new Vector3(stagePos.x, stagePos.y, vehicle.transform.position.z);

        stage.TriggerEnter(vehicle);

        if (stage.isFinishLine)
            TurnEventBus.EmitFinishLineCrossed(vehicle, stage);

        TurnEventBus.EmitStageEntered(vehicle, stage, previousStage, vehicle.progress, isPlayerChoice);
    }
    
    // ==================== COMBAT ====================
    
    public List<Vehicle> GetValidTargets(Vehicle attacker)
    {
        if (attacker == null || attacker.currentStage == null)
            return new List<Vehicle>();

        var validTargets = new List<Vehicle>();
        foreach (var v in vehicles)
        {
            if (v == attacker) continue;
            if (v.currentStage != attacker.currentStage) continue;
            if (v.Status != VehicleStatus.Active) continue;
            validTargets.Add(v);
        }

        return validTargets;
    }
}