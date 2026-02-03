using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using Assets.Scripts.Managers;
using Assets.Scripts.Managers.Logging;
using Assets.Scripts.Managers.TurnPhases;
using Assets.Scripts.Logging;
using Assets.Scripts.Stages;

[RequireComponent(typeof(PlayerController))]

/// <summary>
/// Main game coordinator (Facade Pattern).
/// Manages game initialization and provides simple API to subsystems.
/// 
/// Phase processing is handled by TurnStateMachine via Chain of Responsibility.
/// UI refresh is handled by UIManager subscribing to TurnEventBus.
/// Vehicle destruction is handled via events:
/// - TurnStateMachine removes from turn order
/// - GameManager checks for game over (all player vehicles destroyed)
/// 
/// Supports multiple player-controlled vehicles - each gets their own PlayerAction phase.
/// 
/// Responsibilities:
/// - Initialize game systems
/// - Start/resume state machine execution
/// - Handle player turn completion callback
/// - Check game over conditions (all player vehicles destroyed)
/// - Provide public API for external systems
/// </summary>
public class GameManager : MonoBehaviour
{
    public Stage entryStage;

    [Header("Game Over UI")]
    [Tooltip("Optional - status text for game over message")]
    public TextMeshProUGUI statusNotesText;

    // Controllers
    private TurnStateMachine stateMachine;
    private TurnService turnController;
    private PlayerController playerController;
    private TurnEventLogger eventLogger;
    
    // Phase context (passed to handlers)
    private TurnPhaseContext phaseContext;
    
    // Cached references
    private List<Stage> stages;
    private bool isGameOver = false;

    // ==================== INITIALIZATION ====================

    void Start()
    {
        InitializeGame();
    }

    private void InitializeGame()
    {
        RaceHistory.ClearHistory();

        stages = new List<Stage>(FindObjectsByType<Stage>(FindObjectsSortMode.None));
        List<Vehicle> vehicles = new(FindObjectsByType<Vehicle>(FindObjectsSortMode.None));

        InitializeVehiclePositions(vehicles);
        InitializeControllers(vehicles);
        
        // Create phase context for the handler chain
        phaseContext = new TurnPhaseContext(stateMachine, turnController, playerController);
        
        // Start the game - state machine drives itself via chain of responsibility
        // UI refresh is handled by UIManager subscribing to TurnEventBus events
        stateMachine.Run(phaseContext);
    }

    private void InitializeVehiclePositions(List<Vehicle> vehicles)
    {
        foreach (var vehicle in vehicles)
        {
            Stage startStage = entryStage != null ? entryStage : (stages.Count > 0 ? stages[0] : null);
            vehicle.progress = 0;
            
            vehicle.currentStage = startStage;
            if (startStage != null)
            {
                Vector3 stagePos = startStage.transform.position;
                vehicle.transform.position = new Vector3(stagePos.x, stagePos.y, vehicle.transform.position.z);
                
                if (!startStage.vehiclesInStage.Contains(vehicle))
                {
                    startStage.vehiclesInStage.Add(vehicle);
                }
                
                if (startStage.lanes != null && startStage.lanes.Count > 0)
                {
                    startStage.AssignVehicleToDefaultLane(vehicle);
                }
            }
        }
    }

    private void InitializeControllers(List<Vehicle> vehicles)
    {
        // Initialize event logger
        eventLogger = new TurnEventLogger();
        eventLogger.LogRaceInitialized(vehicles.Count, stages.Count);
        
        foreach (var vehicle in vehicles)
        {
            Stage startStage = vehicle.currentStage;
            eventLogger.LogVehiclePlaced(vehicle, startStage);
            eventLogger.LogCrewComposition(vehicle, startStage);
        }
        
        // Initialize state machine
        stateMachine = new TurnStateMachine();
        stateMachine.Initialize(vehicles);
        
        // Subscribe logger to TurnEventBus (single subscription point for ALL events)
        eventLogger.SetStateMachineReference(stateMachine);
        eventLogger.SubscribeToTurnEventBus();
        
        // Subscribe GameManager to events for game rules
        TurnEventBus.OnGameOver += HandleGameOver;
        TurnEventBus.OnVehicleDestroyed += CheckGameOverCondition;
        
        eventLogger.LogTurnOrderEstablished(stateMachine.AllVehicles);
        
        // Initialize turn controller (plain C# service, not MonoBehaviour)
        turnController = new TurnService(new List<Vehicle>(stateMachine.AllVehicles));

        // Initialize player controller (works with any player-controlled vehicle)
        playerController = GetComponent<PlayerController>();
        if (playerController == null)
        {
            Debug.LogError("PlayerController component not found!");
            return;
        }
        playerController.Initialize(turnController, stateMachine, OnPlayerTurnComplete);
    }

    // ==================== PLAYER TURN CALLBACK ====================

    /// <summary>
    /// Called by PlayerController when player ends their turn.
    /// Resumes the state machine from TurnEnd phase.
    /// </summary>
    private void OnPlayerTurnComplete()
    {
        // Resume state machine - it will transition to TurnEnd and continue running
        // UI refresh is handled by UIManager subscribing to TurnEventBus events
        stateMachine.Resume(phaseContext, TurnPhase.TurnEnd);
    }

    // ==================== EVENT HANDLERS ====================

    /// <summary>
    /// Check if all player vehicles are destroyed - trigger game over.
    /// Called via TurnEventBus.OnVehicleDestroyed.
    /// </summary>
    private void CheckGameOverCondition(Vehicle destroyedVehicle)
    {
        if (destroyedVehicle == null || isGameOver) return;
        
        // Only check if a player-controlled vehicle was destroyed
        if (destroyedVehicle.controlType != ControlType.Player) return;
        
        // Check if any player vehicles remain in the race
        bool anyPlayerVehiclesRemain = stateMachine.AllVehicles
            .Any(v => v.controlType == ControlType.Player && v.Status != VehicleStatus.Destroyed);
        
        if (!anyPlayerVehiclesRemain)
        {
            isGameOver = true;
            if (phaseContext != null)
            {
                phaseContext.IsGameOver = true;
            }
            
            // Transition to GameOver phase (will emit OnGameOver event, TurnEventLogger will log it)
            stateMachine.TransitionTo(TurnPhase.GameOver);
        }
    }

    private void HandleGameOver()
    {
        // Update status text if available (simple game over message)
        if (statusNotesText != null)
        {
            statusNotesText.text = "<color=#FF0000><b>GAME OVER</b></color>\nAll player vehicles have been destroyed!";
        }
        
        // Game over logged by TurnEventLogger via OnGameOver event
    }

    // ==================== PUBLIC API ====================

    public List<Vehicle> GetVehicles()
    {
        if (stateMachine == null) return new List<Vehicle>();
        return new List<Vehicle>(stateMachine.AllVehicles);
    }
    
    /// <summary>
    /// Get all player-controlled vehicles currently in the race.
    /// </summary>
    public List<Vehicle> GetPlayerVehicles()
    {
        if (stateMachine == null) return new List<Vehicle>();
        return stateMachine.AllVehicles
            .Where(v => v.controlType == ControlType.Player)
            .ToList();
    }
    
    public TurnStateMachine GetStateMachine() => stateMachine;
    public TurnService GetTurnController() => turnController;
}