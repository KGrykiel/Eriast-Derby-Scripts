using System.Collections.Generic;
using TMPro;
using UnityEngine;
using RacingGame.Events;
using EventType = RacingGame.Events.EventType; // Add this

/// <summary>
/// Main game coordinator. Manages game initialization and delegates to specialized controllers.
/// Coordinates between TurnController (turn logic) and PlayerController (player input/UI).
/// </summary>
public class GameManager : MonoBehaviour
{
    public Stage entryStage;

    [Header("UI References")]
    public TextMeshProUGUI statusNotesText;
    public RaceLeaderboard raceLeaderboard;
    public VehicleInspectorPanel vehicleInspectorPanel;
    public OverviewPanel overviewPanel; // Add overview panel
    public FocusPanel focusPanel; // Add focus panel

    private List<Stage> stages;
    private TurnController turnController;
    private PlayerController playerController;
    public Vehicle playerVehicle;

    void Start()
    {
        InitializeGame();
    }

    /// <summary>
    /// Initializes all game systems and starts the first turn.
    /// </summary>
    private void InitializeGame()
    {
        // Clear any previous race history
        RaceHistory.ClearHistory();

        stages = new List<Stage>(FindObjectsByType<Stage>(FindObjectsSortMode.None));
        List<Vehicle> vehicles = new List<Vehicle>(FindObjectsByType<Vehicle>(FindObjectsSortMode.None));

        playerVehicle = vehicles.Find(v => v.controlType == ControlType.Player);

        // Log race start
        RaceHistory.Log(
            EventType.System,
            EventImportance.High,
            $"Race initialized with {vehicles.Count} vehicles and {stages.Count} stages"
        );

        InitializeVehiclePositions(vehicles);
        InitializeControllers(vehicles);

        UpdateStatusText();

        // Log first turn
        RaceHistory.AdvanceTurn();

        NextTurn();
    }

    /// <summary>
    /// Places all vehicles at their starting stage.
    /// </summary>
    private void InitializeVehiclePositions(List<Vehicle> vehicles)
    {
        foreach (var vehicle in vehicles)
        {
            Stage startStage = entryStage != null ? entryStage : (stages.Count > 0 ? stages[0] : null);
            vehicle.progress = 0f;
            vehicle.SetCurrentStage(startStage);

            // Old logging (keep for backwards compatibility)
            SimulationLogger.LogEvent($"{vehicle.vehicleName} placed at stage: {startStage?.stageName ?? "None"}");

            // New structured logging
            RaceHistory.Log(
                EventType.System,
                EventImportance.Low,
                $"{vehicle.vehicleName} placed at starting position",
                startStage,
                vehicle
            );
        }
    }

    /// <summary>
    /// Initializes TurnController and PlayerController.
    /// </summary>
    private void InitializeControllers(List<Vehicle> vehicles)
    {
        turnController = gameObject.AddComponent<TurnController>();
        turnController.Initialize(vehicles);

        playerController = GetComponent<PlayerController>();
        if (playerController == null)
        {
            Debug.LogError("PlayerController component not found! Add it to the GameManager GameObject in the Inspector.");
            return;
        }

        playerController.Initialize(playerVehicle, turnController, OnPlayerTurnComplete);
    }

    /// <summary>
    /// Processes turns in a loop until reaching the player's turn.
    /// No recursion, no Invoke() - pure iterative approach for maximum performance.
    /// </summary>
    public void NextTurn()
    {
        // Keep processing turns until we hit the player or run out of vehicles
        while (turnController.AllVehicles.Count > 0)
        {
            var vehicle = turnController.CurrentVehicle;

            // Skip invalid vehicles (destroyed or no stage)
            if (turnController.ShouldSkipTurn(vehicle))
            {
                turnController.AdvanceTurn();
                continue;
            }

            // If it's the player's turn, stop and wait for input
            if (vehicle == playerVehicle)
            {
                // Log player turn start
                RaceHistory.Log(
                    EventType.System,
                    EventImportance.Medium,
                    $"{vehicle.vehicleName}'s turn (Player)",
                    vehicle.currentStage,
                    vehicle
                );

                // Process movement for player
                turnController.ProcessMovement(vehicle);
                playerController.ProcessPlayerMovement();
                UpdateStatusText();

                // Refresh leaderboard
                if (raceLeaderboard != null)
                    raceLeaderboard.RefreshLeaderboard();

                return; // Wait for player input
            }

            // AI turn
            RaceHistory.Log(
                EventType.System,
                EventImportance.Low,
                $"{vehicle.vehicleName}'s turn (AI)",
                vehicle.currentStage,
                vehicle
            );

            turnController.ProcessAITurn(vehicle);
            turnController.AdvanceTurn();
            UpdateStatusText();
        }

        // Game over
        RaceHistory.Log(
            EventType.System,
            EventImportance.Critical,
            "Race ended: No vehicles remaining"
        );

        Debug.Log("Game Over: No vehicles remaining!");

        if (raceLeaderboard != null)
            raceLeaderboard.RefreshLeaderboard();
    }

    /// <summary>
    /// Called by PlayerController when the player completes their turn.
    /// Resumes turn processing from where it left off.
    /// </summary>
    private void OnPlayerTurnComplete()
    {
        turnController.AdvanceTurn();

        // Advance turn counter in history
        RaceHistory.AdvanceTurn();

        // Refresh all UI panels
        if (raceLeaderboard != null)
          raceLeaderboard.RefreshLeaderboard();
        
    if (vehicleInspectorPanel != null)
  vehicleInspectorPanel.OnTurnChanged();
        
        if (overviewPanel != null)
            overviewPanel.RefreshPanel();
      
  if (focusPanel != null)
   focusPanel.RefreshPanel();

        NextTurn();
    }

    /// <summary>
    /// Updates the status text UI with current vehicle positions and progress.
    /// </summary>
    private void UpdateStatusText()
    {
        if (statusNotesText == null) return;

        string statusText = "";
        foreach (var vehicle in turnController.AllVehicles)
        {
            if (vehicle.currentStage == null) continue;
            statusText += $"{vehicle.vehicleName}: {vehicle.currentStage.stageName} ({vehicle.progress:0.0}/{vehicle.currentStage.length})\n";
        }
        statusNotesText.text = statusText;
    }

    /// <summary>
    /// Public API: Returns read-only access to simulation log.
    /// </summary>
    public IReadOnlyList<string> GetSimulationLog() => SimulationLogger.Log;

    /// <summary>
    /// Public API: Returns a copy of all vehicles in the game.
    /// Returns empty list if TurnController not initialized yet.
    /// </summary>
    public List<Vehicle> GetVehicles()
    {
        // Add null check
        if (turnController == null || turnController.AllVehicles == null)
        {
            return new List<Vehicle>(); // Return empty list instead of null
        }

        return new List<Vehicle>(turnController.AllVehicles);
    }
}