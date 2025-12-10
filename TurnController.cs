using System.Collections.Generic;
using UnityEngine;
using RacingGame.Events;
using EventType = RacingGame.Events.EventType;

/// <summary>
/// Handles turn order, turn progression, and vehicle movement.
/// Separated from GameManager for better organization and testability.
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
        
        // Regenerate energy for current vehicle at start of their turn
        if (CurrentVehicle != null && CurrentVehicle.Status == VehicleStatus.Active)
        {
            CurrentVehicle.RegenerateEnergy();
        }
        
        return newRoundStarted;
    }

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
    /// Process a turn for an AI vehicle: add movement, update modifiers, and auto-move through stages.
    /// </summary>
    public void ProcessAITurn(Vehicle vehicle)
    {
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
            return;
        }
        
        // Add movement
        float speed = vehicle.GetAttribute(Attribute.Speed);
        vehicle.progress += speed;
        vehicle.UpdateModifiers();

        // Movement logging removed - only log significant events (stage transitions)
        // This prevents granular low-importance clutter

        // Auto-move through stages
        while (vehicle.progress >= vehicle.currentStage.length && vehicle.currentStage.nextStages.Count > 0)
        {
            vehicle.progress -= vehicle.currentStage.length;
            var options = vehicle.currentStage.nextStages;
            Stage nextStage = options[Random.Range(0, options.Count)];

            Stage previousStage = vehicle.currentStage;
            vehicle.SetCurrentStage(nextStage);

            // Single comprehensive stage transition log
            EventImportance importance = vehicle.controlType == ControlType.Player
                ? EventImportance.Medium
                : EventImportance.Low;

            RaceHistory.Log(
                EventType.Movement,
                importance,
                $"{vehicle.vehicleName} moved from {previousStage.stageName} to {nextStage.stageName} ({vehicle.progress:F1}m carried over)",
                nextStage,
                vehicle
            ).WithMetadata("previousStage", previousStage.stageName)
             .WithMetadata("newStage", nextStage.stageName)
             .WithMetadata("carriedProgress", vehicle.progress)
             .WithMetadata("stageLength", nextStage.length)
             .WithMetadata("isFinishLine", nextStage.isFinishLine);
        }
    }

    /// <summary>
    /// Process movement for a vehicle at a specific stage.
    /// Used by player controller to move through linear paths.
    /// </summary>
    public void ProcessMovement(Vehicle vehicle)
    {
        if (vehicle == null || vehicle.currentStage == null) return;
        
        // Check if vehicle can operate
        if (!vehicle.IsOperational())
        {
            string reason = vehicle.GetNonOperationalReason();
            Debug.LogWarning($"[TurnController] {vehicle.vehicleName} cannot move: {reason}");
            return;
        }

        float speed = vehicle.GetAttribute(Attribute.Speed);
        vehicle.progress += speed;
        vehicle.UpdateModifiers();

        // Movement logging removed - too granular
        // Stage transitions are logged elsewhere
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
            $"{vehicle.vehicleName} chose {stage.stageName} ({vehicle.progress:F1}m carried over)",
            stage,
            vehicle
        ).WithMetadata("previousStage", previousStage?.stageName ?? "None")
         .WithMetadata("selectedStage", stage.stageName)
         .WithMetadata("playerChoice", true)
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