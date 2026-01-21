using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using EventType = Assets.Scripts.Logging.EventType;
using Assets.Scripts.Logging;

/// <summary>
/// Main game coordinator. Manages game initialization and delegates to specialized controllers.
/// Coordinates between TurnController (turn logic) and PlayerController (player input/UI).
/// </summary>
public class GameManager : MonoBehaviour
{
    public Stage entryStage;

    [Header("UI References")]
    public TextMeshProUGUI statusNotesText;
    public VehicleInspectorPanel vehicleInspectorPanel;
    public OverviewPanel overviewPanel;
    public FocusPanel focusPanel;

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

        // Don't advance turn here - we're starting Round 1
        // RaceHistory turn counter starts at 1 by default

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

            // Log starting position
            RaceHistory.Log(
                EventType.System,
                EventImportance.Low,
                $"{vehicle.vehicleName} placed at starting position",
                startStage,
                vehicle
            );
            
            // Log crew composition if vehicle has seats
            if (vehicle.seats.Count > 0)
            {
                var crewList = vehicle.seats
                    .Where(s => s?.assignedCharacter != null)
                    .Select(s => $"{s.assignedCharacter.characterName} ({s.seatName})")
                    .ToList();
                
                if (crewList.Count > 0)
                {
                    RaceHistory.Log(
                        EventType.System,
                        EventImportance.Medium,
                        $"{vehicle.vehicleName} crew: {string.Join(", ", crewList)}",
                        startStage,
                        vehicle
                    ).WithMetadata("crewCount", crewList.Count)
                     .WithMetadata("seatCount", vehicle.seats.Count);
                }
            }
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

        playerController.Initialize(playerVehicle, turnController, this, OnPlayerTurnComplete);
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
                bool skipRoundStarted = turnController.AdvanceTurn();
                
                // Only advance RaceHistory turn counter when a new round starts
                if (skipRoundStarted)
                {
                    RaceHistory.AdvanceTurn();
                }
                
                continue;
            }

            // If it's the player's turn, stop and wait for input
            if (vehicle == playerVehicle)
            {
                StartPlayerTurn();
                return; // Wait for player to end turn
            }

            // AI turn
            RaceHistory.Log(
                EventType.System,
                EventImportance.Low,
                $"{vehicle.vehicleName}'s turn (AI)",
                vehicle.currentStage,
                vehicle
            );

            ProcessAITurn(vehicle);
            
            bool aiRoundStarted = turnController.AdvanceTurn();
            
            // Only advance RaceHistory turn counter when a new round starts
            if (aiRoundStarted)
            {
                RaceHistory.AdvanceTurn();
            }
            
            UpdateStatusText();
            RefreshAllPanels();
        }

        // Game over
        RaceHistory.Log(
            EventType.System,
            EventImportance.Critical,
            "Race ended: No vehicles remaining"
        );

        Debug.Log("Game Over: No vehicles remaining!");
        RefreshAllPanels();
    }

    /// <summary>
    /// Starts the player's turn. Applies movement and shows action UI.
    /// </summary>
    private void StartPlayerTurn()
    {
        // Log player turn start
        RaceHistory.Log(
            EventType.System,
            EventImportance.Medium,
            $"{playerVehicle.vehicleName}'s turn begins",
            playerVehicle.currentStage,
            playerVehicle
        );

        // Apply movement at start of turn
        turnController.ProcessMovement(playerVehicle);
        
        // Handle stage transitions
        playerController.ProcessPlayerMovement();
        
        UpdateStatusText();
        RefreshAllPanels();

        // Show player UI (handled by PlayerController)
    }

    /// <summary>
    /// Processes an AI vehicle's turn including actions and movement.
    /// </summary>
    private void ProcessAITurn(Vehicle vehicle)
    {
        // Process AI movement
        turnController.ProcessAITurn(vehicle);
        
        // Future: Add AI action selection and execution here
        // For now, AI just moves (existing behavior)
    }

    /// <summary>
    /// Called by PlayerController when the player completes their turn.
    /// Resumes turn processing from where it left off.
    /// </summary>
    private void OnPlayerTurnComplete()
    {
        // Log turn end
        RaceHistory.Log(
            EventType.System,
            EventImportance.Low,
            $"{playerVehicle.vehicleName}'s turn ends",
            playerVehicle.currentStage,
            playerVehicle
        );

        // Advance turn and check if new round started
        bool newRoundStarted = turnController.AdvanceTurn();

        // Only advance RaceHistory turn counter when a new round starts
        if (newRoundStarted)
        {
            RaceHistory.AdvanceTurn();
        }

        RefreshAllPanels();

        // Continue to next turn
        NextTurn();
    }

    /// <summary>
    /// Refreshes all UI panels at once.
    /// Called after turn changes or significant game state updates.
    /// </summary>
    public void RefreshAllPanels()
    {
        if (vehicleInspectorPanel != null)
            vehicleInspectorPanel.OnTurnChanged();
        
        if (overviewPanel != null)
            overviewPanel.RefreshPanel();
      
        if (focusPanel != null)
            focusPanel.RefreshPanel();
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
    /// Deprecated: Use RaceHistory instead.
    /// </summary>
    [System.Obsolete("Use RaceHistory instead of SimulationLogger")]
    public IReadOnlyList<string> GetSimulationLog() => new List<string>();

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