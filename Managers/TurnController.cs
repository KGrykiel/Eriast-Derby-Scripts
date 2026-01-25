using System.Collections.Generic;
using UnityEngine;
using EventType = Assets.Scripts.Logging.EventType;
using Assets.Scripts.Logging;

/// <summary>
/// Handles turn order, turn progression, and vehicle movement.
/// Implements 3-stage turn system:
/// 1. StartTurn - Regen power, pay continuous costs, reset flags
/// 2. Action Phase - Player/AI actions, movement can be triggered anytime
/// 3. EndTurn - Auto-move if not moved, status effects tick
/// </summary>
public class TurnController : MonoBehaviour
{
    private List<Vehicle> vehicles;
    private int currentTurnIndex = 0;
    private int currentRound = 1;

    public Vehicle CurrentVehicle => vehicles != null && vehicles.Count > 0 ? vehicles[currentTurnIndex] : null;
    public IReadOnlyList<Vehicle> AllVehicles => vehicles;
    public int CurrentRound => currentRound;

    /// <summary>
    /// Initialize the turn controller with a list of vehicles.
    /// Rolls initiative and sorts vehicles by initiative order.
    /// </summary>
    public void Initialize(List<Vehicle> vehicleList)
    {
        vehicles = new List<Vehicle>(vehicleList);
        currentRound = 1;

        // Roll initiative for each vehicle
        Dictionary<Vehicle, int> initiativeRolls = new Dictionary<Vehicle, int>();
        foreach (var vehicle in vehicles)
        {
            int initiative = Random.Range(1, 101);
            initiativeRolls[vehicle] = initiative;

            // Log to event system
            RaceHistory.Log(
                EventType.System,
                EventImportance.Low,
                $"{vehicle.vehicleName} rolled initiative: {initiative}",
                null,
                vehicle
            ).WithMetadata("initiative", initiative);
        }

        // Sort vehicles by initiative (descending)
        vehicles.Sort((a, b) => initiativeRolls[b].CompareTo(initiativeRolls[a]));

        string turnOrder = string.Join(", ", vehicles.ConvertAll(v => v.vehicleName));

        RaceHistory.Log(
            EventType.System,
            EventImportance.Medium,
            $"Turn order established: {turnOrder}"
        );

        currentTurnIndex = 0;
    }

    /// <summary>
    /// Advance to the next vehicle in turn order.
    /// Returns true if a new round has started.
    /// </summary>
    public bool AdvanceTurn()
    {
        if (vehicles.Count == 0) return false;
        
        int previousIndex = currentTurnIndex;
        currentTurnIndex = (currentTurnIndex + 1) % vehicles.Count;
        
        // Check if we've wrapped back to the first vehicle (new round)
        bool newRoundStarted = currentTurnIndex < previousIndex;
        
        if (newRoundStarted)
        {
            currentRound++;
            
            // Log round change
            RaceHistory.Log(
                EventType.System,
                EventImportance.Medium,
                $"================= Round {currentRound} begins ==================",
                null
            ).WithMetadata("round", currentRound)
             .WithMetadata("vehicleCount", vehicles.Count);
        }
        
        return newRoundStarted;
    }

    // ==================== 3-STAGE TURN SYSTEM ====================
    
    /// <summary>
    /// Stage 1: Start of turn for a vehicle.
    /// 1. Regenerate power
    /// 2. Reset per-turn power tracking
    /// 3. Draw continuous power for all components (drive at current speed)
    /// 4. Reset movement flag
    /// 5. Apply turn-start status effects
    /// 6. Reset seat states
    /// </summary>
    public void StartTurn(Vehicle vehicle)
    {
        if (vehicle == null) return;
        
        // 1. Regenerate power FIRST (see full resources)
        vehicle.RegenerateEnergy();
        
        // 2. Reset per-turn power tracking
        if (vehicle.powerCore != null)
        {
            vehicle.powerCore.ResetTurnPowerTracking();
        }
        
        // 3. Draw continuous power for all components (drive pays based on CURRENT speed)
        DrawContinuousPowerForAllComponents(vehicle);
        
        // 4. Reset movement flag - player controls when movement happens
        vehicle.hasMovedThisTurn = false;
        vehicle.hasLoggedMovementWarningThisTurn = false;
        
        // 5. Status effects at turn start
        // TODO: Add OnTurnStart() to status effects when implemented
        
        // 6. Reset seat turn states
        vehicle.ResetComponentsForNewTurn();
        
        RaceHistory.Log(
            EventType.System,
            EventImportance.Low,
            $"{vehicle.vehicleName}'s turn begins (Round {currentRound})",
            vehicle.currentStage,
            vehicle
        ).WithMetadata("round", currentRound)
         .WithMetadata("hasMovedYet", false);
    }
    
    /// <summary>
    /// Stage 3: End of turn for a vehicle.
    /// 1. Auto-trigger movement if player hasn't moved yet (mandatory)
    /// 2. Apply turn-end status effects
    /// </summary>
    public void EndTurn(Vehicle vehicle)
    {
        if (vehicle == null) return;
        
        // 1. FORCE movement if player hasn't triggered it yet
        // Movement is mandatory - engine is running, vehicle WILL move
        if (!vehicle.hasMovedThisTurn)
        {
            RaceHistory.Log(
                EventType.Movement,
                EventImportance.Low,
                $"{vehicle.vehicleName} automatically moved (movement not triggered manually)",
                vehicle.currentStage,
                vehicle
            ).WithMetadata("automatic", true);
            
            ExecuteMovement(vehicle);
        }
        
        // 2. Status effects at turn end
        vehicle.UpdateStatusEffects();
        
        RaceHistory.Log(
            EventType.System,
            EventImportance.Low,
            $"{vehicle.vehicleName}'s turn ends",
            vehicle.currentStage,
            vehicle
        );
    }
    
    /// <summary>
    /// Draw continuous power for all components.
    /// Called at turn start - components pay based on CURRENT state (drive pays for current speed).
    /// </summary>
    private void DrawContinuousPowerForAllComponents(Vehicle vehicle)
    {
        if (vehicle.powerCore == null) return;
        
        foreach (var component in vehicle.AllComponents)
        {
            if (component == null || component.isDestroyed || !component.isPowered) continue;
            
            // Get component's power draw (drive will calculate based on current speed)
            int powerCost = component.GetActualPowerDraw();
            
            if (powerCost <= 0) continue;
            
            // Attempt to draw power
            bool success = vehicle.powerCore.DrawPower(
                powerCost, 
                component, 
                "Continuous operation"
            );
            
            if (!success)
            {
                // Insufficient power - component shuts down
                RaceHistory.Log(
                    EventType.Resource,
                    EventImportance.Medium,
                    $"{vehicle.vehicleName}: {component.name} shut down due to insufficient power (needs {powerCost}, have {vehicle.powerCore.currentEnergy})",
                    vehicle.currentStage,
                    vehicle
                ).WithMetadata("component", component.name)
                 .WithMetadata("requiredPower", powerCost)
                 .WithMetadata("reason", "InsufficientPower");
                
                // Component becomes temporarily disabled until power is restored
                component.isPowered = false;
            }
        }
    }
    
    /// <summary>
    /// Execute movement for a vehicle. Can be called manually during action phase or auto-triggered at turn end.
    /// Movement is FREE - power was already paid at turn start by drive continuous draw.
    /// </summary>
    public bool ExecuteMovement(Vehicle vehicle)
    {
        if (vehicle == null) return false;
        
        // Already moved this turn
        if (vehicle.hasMovedThisTurn)
        {
            Debug.LogWarning($"[TurnController] {vehicle.vehicleName} has already moved this turn");
            return false;
        }
        
        // Check if vehicle can move (drive operational + not immobilized)
        if (!vehicle.CanMove())
        {
            if (!vehicle.hasLoggedMovementWarningThisTurn)
            {
                string reason = vehicle.GetCannotMoveReason();
                RaceHistory.Log(
                    EventType.Movement,
                    EventImportance.Medium,
                    $"{vehicle.vehicleName} cannot move: {reason}",
                    vehicle.currentStage,
                    vehicle
                ).WithMetadata("cannotMove", true)
                 .WithMetadata("reason", reason);
                
                vehicle.hasLoggedMovementWarningThisTurn = true;
            }
            
            vehicle.hasMovedThisTurn = true; // Mark as "moved" to prevent spam
            return false;
        }
        
        // NO POWER COST HERE - already paid at turn start by drive continuous draw
        var drive = vehicle.GetDriveComponent();
        float distance = drive?.currentSpeed ?? 0f;
        
        if (vehicle.currentStage != null && distance > 0)
        {
            float oldProgress = vehicle.progress;
            vehicle.progress += distance;
            
            RaceHistory.Log(
                EventType.Movement,
                EventImportance.Low,
                $"{vehicle.vehicleName} moved {distance:F1} units (speed {drive.currentSpeed:F1})",
                vehicle.currentStage,
                vehicle
            ).WithMetadata("distance", distance)
             .WithMetadata("speed", drive.currentSpeed)
             .WithMetadata("oldProgress", oldProgress)
             .WithMetadata("newProgress", vehicle.progress);
        }
        
        vehicle.hasMovedThisTurn = true;
        return true;
    }

    
    // ==================== LEGACY & UTILITY METHODS ====================

    /// <summary>
    /// Check if a vehicle's turn should be skipped (destroyed or no stage).
    /// Returns true if turn should be skipped.
    /// </summary>
    public bool ShouldSkipTurn(Vehicle vehicle)
    {
        if (vehicle.currentStage == null)
        {
            RaceHistory.Log(
                EventType.System,
                EventImportance.Low,
                $"{vehicle.vehicleName} skipped (no stage)",
                null,
                vehicle
            );

            return true;
        }

        if (vehicle.Status == VehicleStatus.Destroyed)
        {
            RaceHistory.Log(
                EventType.System,
                EventImportance.Low,
                $"{vehicle.vehicleName} skipped (destroyed)",
                vehicle.currentStage,
                vehicle
            );

            return true;
        }

        return false;
    }

    /// <summary>
    /// Process a complete turn for an AI vehicle.
    /// Uses 3-stage turn system: StartTurn -> Actions -> EndTurn
    /// </summary>
    public void ProcessAITurn(Vehicle vehicle)
    {
        if (vehicle == null) return;
        
        // Stage 1: Start Turn
        StartTurn(vehicle);
        
        // Check if vehicle can operate
        if (!vehicle.IsOperational())
        {
            string reason = vehicle.GetNonOperationalReason();
            RaceHistory.Log(
                EventType.System,
                EventImportance.Medium,
                $"{vehicle.vehicleName} cannot act: {reason}",
                vehicle.currentStage,
                vehicle
            ).WithMetadata("nonOperational", true)
             .WithMetadata("reason", reason);
            
            // Stage 3: End Turn (even if can't act)
            EndTurn(vehicle);
            return;
        }
        
        // Stage 2: Action Phase
        // For AI, we trigger movement immediately (could be changed for tactical AI later)
        ExecuteMovement(vehicle);
        
        // Auto-move through stages if reached end
        while (vehicle.progress >= vehicle.currentStage.length && vehicle.currentStage.nextStages.Count > 0)
        {
            vehicle.progress -= vehicle.currentStage.length;
            var options = vehicle.currentStage.nextStages;
            Stage nextStage = options[Random.Range(0, options.Count)];

            MoveToStage(vehicle, nextStage);
        }
        
        // Stage 3: End Turn
        EndTurn(vehicle);
    }

    /// <summary>
    /// Move a vehicle to a specific stage (used for player stage selection).
    /// </summary>
    public void MoveToStage(Vehicle vehicle, Stage stage)
    {
        if (vehicle == null || stage == null) return;

        Stage previousStage = vehicle.currentStage;
        vehicle.progress -= vehicle.currentStage.length;
        vehicle.SetCurrentStage(stage);

        // Single comprehensive stage transition log
        EventImportance importance = vehicle.controlType == ControlType.Player
            ? EventImportance.Medium
            : EventImportance.Low;

        RaceHistory.Log(
            EventType.Movement,
            importance,
            $"{vehicle.vehicleName} moved to {stage.stageName} ({vehicle.progress:F1}m carried over)",
            stage,
            vehicle
        ).WithMetadata("previousStage", previousStage?.stageName ?? "None")
         .WithMetadata("selectedStage", stage.stageName)
         .WithMetadata("playerChoice", vehicle.controlType == ControlType.Player)
         .WithMetadata("carriedProgress", vehicle.progress);
    }

    /// <summary>
    /// Get list of valid targets for a vehicle (same stage, active, not self).
    /// </summary>
    public List<Vehicle> GetValidTargets(Vehicle attacker)
    {
        if (attacker == null || attacker.currentStage == null)
            return new List<Vehicle>();

        List<Vehicle> validTargets = new List<Vehicle>();
        foreach (var v in vehicles)
        {
            if (v == attacker) continue;
            if (v.currentStage != attacker.currentStage) continue;
            if (v.Status != VehicleStatus.Active) continue;
            validTargets.Add(v);
        }

        return validTargets;
    }

    /// <summary>
    /// Removes a destroyed vehicle from the turn order permanently.
    /// Adjusts turn index to maintain proper turn flow.
    /// </summary>
    public void RemoveDestroyedVehicle(Vehicle vehicle)
    {
        int index = vehicles.IndexOf(vehicle);
        if (index < 0) return;

        vehicles.RemoveAt(index);

        RaceHistory.Log(
            EventType.System,
            EventImportance.High,
            $"{vehicle.vehicleName} removed from turn order",
            vehicle.currentStage,
            vehicle
        );

        // Adjust currentTurnIndex if needed
        if (index < currentTurnIndex)
        {
            currentTurnIndex--;
        }
        else if (index == currentTurnIndex)
        {
            if (currentTurnIndex >= vehicles.Count && vehicles.Count > 0)
            {
                currentTurnIndex = 0;
            }
        }

        if (vehicles.Count > 0 && currentTurnIndex >= vehicles.Count)
            currentTurnIndex = 0;
    }
}