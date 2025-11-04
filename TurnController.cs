using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles turn order, turn progression, and vehicle movement.
/// Separated from GameManager for better organization and testability.
/// </summary>
public class TurnController : MonoBehaviour
{
    private List<Vehicle> vehicles;
    private int currentTurnIndex = 0;

    public Vehicle CurrentVehicle => vehicles != null && vehicles.Count > 0 ? vehicles[currentTurnIndex] : null;
    public IReadOnlyList<Vehicle> AllVehicles => vehicles;

    /// <summary>
    /// Initialize the turn controller with a list of vehicles.
    /// Rolls initiative and sorts vehicles by initiative order.
    /// </summary>
    public void Initialize(List<Vehicle> vehicleList)
    {
        vehicles = new List<Vehicle>(vehicleList);

        // Roll initiative for each vehicle
        Dictionary<Vehicle, int> initiativeRolls = new Dictionary<Vehicle, int>();
        foreach (var vehicle in vehicles)
        {
            int initiative = Random.Range(1, 101);
            initiativeRolls[vehicle] = initiative;
            SimulationLogger.LogEvent($"{vehicle.vehicleName} rolled initiative: {initiative}");
        }

        // Sort vehicles by initiative (descending)
        vehicles.Sort((a, b) => initiativeRolls[b].CompareTo(initiativeRolls[a]));
        SimulationLogger.LogEvent("Turn order: " + string.Join(", ", vehicles.ConvertAll(v => v.vehicleName)));

        currentTurnIndex = 0;
    }

    /// <summary>
    /// Advance to the next vehicle in turn order.
    /// </summary>
    public void AdvanceTurn()
    {
        if (vehicles.Count == 0) return;
        currentTurnIndex = (currentTurnIndex + 1) % vehicles.Count;
    }

    /// <summary>
    /// Check if a vehicle's turn should be skipped (destroyed or no stage).
    /// Returns true if turn should be skipped.
    /// </summary>
    public bool ShouldSkipTurn(Vehicle vehicle)
    {
        if (vehicle.currentStage == null)
        {
            SimulationLogger.LogEvent($"{vehicle.vehicleName} has no current stage. Skipping turn.");
            return true;
        }

        if (vehicle.Status == VehicleStatus.Destroyed)
        {
            SimulationLogger.LogEvent($"{vehicle.vehicleName} is destroyed. Skipping turn.");
            return true;
        }

        return false;
    }

    /// <summary>
    /// Process a turn for an AI vehicle: add movement, update modifiers, and auto-move through stages.
    /// </summary>
    public void ProcessAITurn(Vehicle vehicle)
    {
        // Add movement
        float speed = vehicle.GetAttribute(Attribute.Speed);
        vehicle.progress += speed;
        vehicle.UpdateModifiers();

        // Auto-move through stages
        while (vehicle.progress >= vehicle.currentStage.length && vehicle.currentStage.nextStages.Count > 0)
        {
            vehicle.progress -= vehicle.currentStage.length;
            var options = vehicle.currentStage.nextStages;
            Stage nextStage = options[Random.Range(0, options.Count)];
            vehicle.SetCurrentStage(nextStage);
        }
    }

    /// <summary>
    /// Process movement for a vehicle at a specific stage.
    /// Used by player controller to move through linear paths.
    /// </summary>
    public void ProcessMovement(Vehicle vehicle)
    {
        if (vehicle == null || vehicle.currentStage == null) return;

        // Just add speed-based progress
        float speed = vehicle.GetAttribute(Attribute.Speed);
        vehicle.progress += speed;
        vehicle.UpdateModifiers();
    }

    /// <summary>
    /// Move a vehicle to a specific stage (used for player stage selection).
    /// </summary>
    public void MoveToStage(Vehicle vehicle, Stage stage)
    {
        if (vehicle == null || stage == null) return;

        vehicle.progress -= vehicle.currentStage.length;
        vehicle.SetCurrentStage(stage);
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
        SimulationLogger.LogEvent($"{vehicle.vehicleName} removed from turn order.");
        
        if (index < currentTurnIndex)
        {
            currentTurnIndex--;
        }
        else if (index == currentTurnIndex)
        {
            if (currentTurnIndex >= vehicles.Count && vehicles.Count > 0)
            {
                currentTurnIndex = 0; // Wrap around
            }
        }
        if (vehicles.Count > 0 && currentTurnIndex >= vehicles.Count)
        {
            currentTurnIndex = 0;
        }
    }
}