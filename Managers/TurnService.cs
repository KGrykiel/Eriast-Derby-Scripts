using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts.Managers;
using Assets.Scripts.Stages;

/// <summary>
/// Service class for vehicle operations during turns.
/// Phase handlers orchestrate turn flow and call these utilities.
/// Events are emitted via TurnEventBus.
/// 
/// Plain C# class (not MonoBehaviour) - purely stateless utilities.
/// 
/// Responsibilities:
/// - Movement execution
/// - Power management utilities
/// - Stage transitions
/// - Combat targeting
/// 
/// NOTE: Turn start/end orchestration is handled by TurnStartHandler/TurnEndHandler.
/// This class is just utilities that handlers call.
/// </summary>
public class TurnService
{
    private readonly List<Vehicle> vehicles;
    
    public IReadOnlyList<Vehicle> AllVehicles => vehicles;

    public TurnService(List<Vehicle> vehicleList)
    {
        vehicles = vehicleList ?? new List<Vehicle>();
    }

    // ==================== POWER MANAGEMENT ====================
    
    /// <summary>
    /// Draw continuous power for all components.
    /// Components pay based on CURRENT state (drive pays for current speed).
    /// Emits OnComponentPowerShutdown via TurnEventBus for any components that shut down.
    /// </summary>
    public void DrawContinuousPowerForAllComponents(Vehicle vehicle)
    {
        if (vehicle == null || vehicle.powerCore == null) return;
        
        foreach (var component in vehicle.AllComponents)
        {
            if (component == null || !component.IsOperational) continue;
            
            int powerCost = component.GetActualPowerDraw();
            if (powerCost <= 0) continue;
            
            bool success = vehicle.powerCore.DrawPower(powerCost, component, "Continuous operation");
            
            if (!success)
            {
                TurnEventBus.EmitComponentPowerShutdown(vehicle, component, powerCost, vehicle.powerCore.currentEnergy);
                component.SetManuallyDisabled(true);
            }
        }
    }
    
    // ==================== SPEED/ACCELERATION ====================
    
    /// <summary>
    /// Adjust vehicle speed toward target at start of turn.
    /// Player/AI sets targetSpeed during action phase, this applies the change.
    /// If drive is unpowered/destroyed, applies friction to slow vehicle down.
    /// </summary>
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
    
    /// <summary>
    /// Execute movement for a vehicle.
    /// Movement is FREE - power was already paid at turn start.
    /// Emits movement events via TurnEventBus.
    /// </summary>
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
    
    /// <summary>
    /// Move a vehicle to a specific stage (handles overflow progress).
    /// Handles stage enter/leave events, position updates, and finish line detection.
    /// Emits OnStageEntered via TurnEventBus.
    /// </summary>
    public void MoveToStage(Vehicle vehicle, Stage stage, bool isPlayerChoice = false)
    {
        if (vehicle == null || stage == null) return;

        Stage previousStage = vehicle.currentStage;
        
        // Trigger leave event on old stage
        if (previousStage != null)
        {
            previousStage.TriggerLeave(vehicle);
        }
        
        // Update vehicle state
        vehicle.progress -= previousStage != null ? previousStage.length : 0;
        vehicle.currentStage = stage;
        
        // Update Unity transform position
        Vector3 stagePos = stage.transform.position;
        vehicle.transform.position = new Vector3(stagePos.x, stagePos.y, vehicle.transform.position.z);
        
        // Trigger enter event on new stage
        stage.TriggerEnter(vehicle);
        
        // Check for finish line and emit event (TurnEventLogger will log it)
        if (stage.isFinishLine)
        {
            TurnEventBus.EmitFinishLineCrossed(vehicle, stage);
        }
        
        // Emit event for UI/logging
        TurnEventBus.EmitStageEntered(vehicle, stage, previousStage, vehicle.progress, isPlayerChoice);
    }
    
    // ==================== COMBAT ====================
    
    /// <summary>
    /// Get list of valid targets for a vehicle (same stage, active, not self).
    /// </summary>
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