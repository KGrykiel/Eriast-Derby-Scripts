using System;
using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts.Logging;

/// <summary>
/// Handles vehicle-specific turn operations.
/// State management (rounds, turn order, phases) is handled by TurnStateMachine.
/// 
/// Responsibilities:
/// - Execute turn start/end logic on vehicles
/// - Handle movement execution
/// - Manage power draw for components
/// - Stage transitions
/// - Combat targeting
/// 
/// Fires events for logging (TurnEventLogger subscribes).
/// Called by GameManager during phase transitions.
/// </summary>
public class TurnController : MonoBehaviour
{
    // Reference to vehicles (set during Initialize, shared with TurnStateMachine)
    private List<Vehicle> vehicles;
    
    public IReadOnlyList<Vehicle> AllVehicles => vehicles;
    
    // ==================== EVENTS ====================
    
    /// <summary>Fired when a vehicle auto-moves at turn end</summary>
    public event Action<Vehicle> OnAutoMovement;
    
    /// <summary>Fired when a component shuts down due to insufficient power. Args: (vehicle, component, requiredPower, availablePower)</summary>
    public event Action<Vehicle, VehicleComponent, int, int> OnComponentPowerShutdown;
    
    /// <summary>Fired when movement is blocked. Args: (vehicle, reason)</summary>
    public event Action<Vehicle, string> OnMovementBlocked;
    
    /// <summary>Fired when movement executes. Args: (vehicle, distance, speed, oldProgress, newProgress)</summary>
    public event Action<Vehicle, int, int, int, int> OnMovementExecuted;
    
    /// <summary>Fired when vehicle enters a new stage. Args: (vehicle, newStage, previousStage, carriedProgress, isPlayerChoice)</summary>
    public event Action<Vehicle, Stage, Stage, int, bool> OnStageEntered;

    /// <summary>
    /// Initialize with vehicle list (same list as TurnStateMachine uses).
    /// </summary>
    public void Initialize(List<Vehicle> vehicleList)
    {
        vehicles = vehicleList;
    }

    // ==================== TURN EXECUTION ====================
    
    /// <summary>
    /// Execute turn start logic for a vehicle.
    /// Called by GameManager during TurnStart phase.
    /// 
    /// Order:
    /// 1. Regenerate power
    /// 2. Reset per-turn power tracking
    /// 3. Accelerate (TODO: will be player/AI controlled later)
    /// 4. Draw continuous power for all components
    /// 5. Reset movement flag
    /// 6. Reset seat/component states
    /// </summary>
    public void StartTurn(Vehicle vehicle)
    {
        if (vehicle == null) return;
        
        // 1. Regenerate power FIRST (see full resources before paying costs)
        if (vehicle.powerCore != null && !vehicle.powerCore.isDestroyed)
        {
            vehicle.powerCore.RegenerateEnergy();
        }
        
        // 2. Reset per-turn power tracking
        if (vehicle.powerCore != null)
        {
            vehicle.powerCore.ResetTurnPowerTracking();
        }
        
        // 3. Accelerate - for now, everyone goes full throttle (TODO: player/AI control)
        AccelerateVehicle(vehicle);
        
        // 4. Draw continuous power for all components (drive pays based on CURRENT speed)
        DrawContinuousPowerForAllComponents(vehicle);
        
        // 5. Reset movement flag - player controls when movement happens
        vehicle.hasMovedThisTurn = false;
        vehicle.hasLoggedMovementWarningThisTurn = false;
        
        // 6. Reset seat turn states
        vehicle.ResetComponentsForNewTurn();

        // 7. Status effects at turn start
        vehicle.UpdateStatusEffects();
    }
    
    /// <summary>
    /// Adjust vehicle speed toward target at start of turn.
    /// Player/AI sets targetSpeed during action phase, this applies the change.
    /// Speed changes gradually based on acceleration/deceleration limits.
    /// If drive is unpowered/destroyed, applies friction to slow vehicle down.
    /// </summary>
    private void AccelerateVehicle(Vehicle vehicle)
    {
        var drive = vehicle.GetDriveComponent();
        if (drive == null) return;
        
        if (!drive.IsOperational)
        {
            // Unpowered drive: vehicle coasts to a stop via friction
            drive.ApplyFriction();
            return;
        }
        
        // Move currentSpeed toward targetSpeed (respects acceleration limits)
        drive.AdjustSpeedTowardTarget();
    }
    
    /// <summary>
    /// Execute turn end logic for a vehicle.
    /// Called by GameManager during TurnEnd phase.
    /// 
    /// Order:
    /// 1. Auto-trigger movement if not moved (mandatory)
    /// 2. Update status effects
    /// </summary>
    public void EndTurn(Vehicle vehicle)
    {
        if (vehicle == null) return;
        
        // 1. FORCE movement if player hasn't triggered it yet
        if (!vehicle.hasMovedThisTurn)
        {
            OnAutoMovement?.Invoke(vehicle);
            ExecuteMovement(vehicle);
        }
    }
    
    // ==================== POWER MANAGEMENT ====================
    
    /// <summary>
    /// Draw continuous power for all components.
    /// Components pay based on CURRENT state (drive pays for current speed).
    /// </summary>
    private void DrawContinuousPowerForAllComponents(Vehicle vehicle)
    {
        if (vehicle.powerCore == null) return;
        
        foreach (var component in vehicle.AllComponents)
        {
            if (component == null || !component.IsOperational) continue;
            
            int powerCost = component.GetActualPowerDraw();
            if (powerCost <= 0) continue;
            
            bool success = vehicle.powerCore.DrawPower(powerCost, component, "Continuous operation");
            
            if (!success)
            {
                OnComponentPowerShutdown?.Invoke(vehicle, component, powerCost, vehicle.powerCore.currentEnergy);
                component.SetManuallyDisabled(true);
            }
        }
    }
    
    // ==================== MOVEMENT ====================
    
    /// <summary>
    /// Execute movement for a vehicle.
    /// Movement is FREE - power was already paid at turn start.
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
                OnMovementBlocked?.Invoke(vehicle, reason);
                vehicle.hasLoggedMovementWarningThisTurn = true;
            }
            
            vehicle.hasMovedThisTurn = true;
            return false;
        }
        
        var drive = vehicle.GetDriveComponent();
        int distance = drive != null ? drive.GetCurrentSpeed() : 0;  // INTEGER: D&D-style discrete movement
        
        if (vehicle.currentStage != null && distance > 0)
        {
            int oldProgress = vehicle.progress;
            vehicle.progress += distance;
            OnMovementExecuted?.Invoke(vehicle, distance, drive.GetCurrentSpeed(), oldProgress, vehicle.progress);
        }
        
        vehicle.hasMovedThisTurn = true;
        return true;
    }
    
    /// <summary>
    /// Move a vehicle to a specific stage (handles overflow progress).
    /// Handles stage enter/leave events, position updates, and finish line detection.
    /// </summary>
    public void MoveToStage(Vehicle vehicle, Stage stage)
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
        if (stage != null)
        {
            Vector3 stagePos = stage.transform.position;
            vehicle.transform.position = new Vector3(stagePos.x, stagePos.y, vehicle.transform.position.z);
        }
        
        // Trigger enter event on new stage
        if (stage != null)
        {
            stage.TriggerEnter(vehicle);
            
            // Check for finish line
            if (stage.isFinishLine)
            {
                RaceHistory.Log(
                    Assets.Scripts.Logging.EventType.FinishLine,
                    EventImportance.Critical,
                    $"[FINISH] {vehicle.vehicleName} crossed the finish line!",
                    stage,
                    vehicle
                );
            }
        }
        
        // Fire event for UI/logging
        bool isPlayerChoice = vehicle.controlType == ControlType.Player;
        OnStageEntered?.Invoke(vehicle, stage, previousStage, vehicle.progress, isPlayerChoice);
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