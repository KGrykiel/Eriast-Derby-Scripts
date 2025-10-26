using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Main game coordinator. Manages game initialization and delegates to specialized controllers.
/// Coordinates between TurnController (turn logic) and PlayerController (player input/UI).
/// </summary>
public class GameManager : MonoBehaviour
{
    public Stage entryStage;

    [Header("UI References")]
    public TextMeshProUGUI statusNotesText;

    private List<Stage> stages;
    private TurnController turnController;
    private PlayerController playerController;
    public Vehicle playerVehicle;

    void Start()
    {
        Debug.Log($"<color=cyan>===== FRAME {Time.frameCount} START =====</color>");
        
        // Log every single operation
        Debug.Log($"Frame {Time.frameCount}: About to call InitializeGame()");
        InitializeGame();
        Debug.Log($"Frame {Time.frameCount}: InitializeGame() completed");
        
        Debug.Log($"<color=cyan>===== FRAME {Time.frameCount} END =====</color>");
    }

    void Update()
    {
        // Log every frame for first 5 frames
        if (Time.frameCount <= 5)
        {
            Debug.Log($"<color=yellow>Frame {Time.frameCount} Update() called</color>");
        }
    }

    void LateUpdate()
    {
        if (Time.frameCount <= 5)
        {
            Debug.Log($"<color=orange>Frame {Time.frameCount} LateUpdate() called</color>");
        }
    }

    /// <summary>
    /// Initializes all game systems and starts the first turn.
    /// </summary>
    private void InitializeGame()
    {
        stages = new List<Stage>(FindObjectsByType<Stage>(FindObjectsSortMode.None));
        List<Vehicle> vehicles = new List<Vehicle>(FindObjectsByType<Vehicle>(FindObjectsSortMode.None));

        playerVehicle = vehicles.Find(v => v.controlType == ControlType.Player);

        InitializeVehiclePositions(vehicles);
        InitializeControllers(vehicles);

        UpdateStatusText();
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
            SimulationLogger.LogEvent($"{vehicle.vehicleName} placed at stage: {startStage?.stageName ?? "None"}");
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
                continue; // Skip to next vehicle in loop
            }

            // Process movement for current vehicle
            turnController.ProcessMovement(vehicle);

            // If it's the player's turn, stop and wait for input
            if (vehicle == playerVehicle)
            {
                playerController.ProcessPlayerMovement();
                UpdateStatusText();
                return; // Exit method, waiting for player action
            }

            // AI turn - process and move to next vehicle
            turnController.ProcessAITurn(vehicle);
            turnController.AdvanceTurn();
            UpdateStatusText();

            // Loop continues immediately to next AI turn
        }

        // Only reached if all vehicles are destroyed
        Debug.Log("Game Over: No vehicles remaining!");
    }

    /// <summary>
    /// Called by PlayerController when the player completes their turn.
    /// Resumes turn processing from where it left off.
    /// </summary>
    private void OnPlayerTurnComplete()
    {
        turnController.AdvanceTurn();
        NextTurn(); // Resume the loop
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
    /// </summary>
    public List<Vehicle> GetVehicles() => new List<Vehicle>(turnController.AllVehicles);
}