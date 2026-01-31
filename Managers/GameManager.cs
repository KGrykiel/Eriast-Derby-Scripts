using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Assets.Scripts.Managers;
using Assets.Scripts.Managers.Logging;
using Assets.Scripts.Logging;
using Assets.Scripts.Stages;

[RequireComponent(typeof(PlayerController))]

/// <summary>
/// Main game coordinator. Manages game initialization and delegates to specialized controllers.
/// Uses TurnStateMachine for phase management and TurnController for vehicle operations.
/// 
/// Responsibilities:
/// - Initialize game systems
/// - Process state machine phases
/// - Orchestrate player/AI turn flow
/// 
/// Note: Logging handled by TurnEventLogger, UI handled by panels subscribing to events.
/// </summary>
public class GameManager : MonoBehaviour
{
    public Stage entryStage;

    [Header("UI References")]
    public TextMeshProUGUI statusNotesText;
    public VehicleInspectorPanel vehicleInspectorPanel;
    public OverviewPanel overviewPanel;
    public FocusPanel focusPanel;

    // Controllers
    private TurnStateMachine stateMachine;
    private TurnController turnController;
    private PlayerController playerController;
    private TurnEventLogger eventLogger;
    
    // Cached references
    private List<Stage> stages;
    public Vehicle playerVehicle;
    private bool isGameOver = false; // Prevent re-entry during game over processing

    // ==================== PUBLIC API ====================
    
    /// <summary>
    /// Handle vehicle destruction immediately. Called by Vehicle.MarkAsDestroyed().
    /// Removes vehicle from turn order right away (mid-turn safe).
    /// Ends game if player vehicle is destroyed.
    /// </summary>
    public void HandleVehicleDestroyed(Vehicle vehicle)
    {
        if (vehicle == null || stateMachine == null) return;
        if (isGameOver) return; // Prevent re-entry during game over processing
        
        Debug.Log($"[GameManager] {vehicle.vehicleName} destroyed - removing from turn order immediately");
        stateMachine.RemoveVehicle(vehicle);
        
        // Check if player vehicle was destroyed - GAME OVER
        if (vehicle == playerVehicle)
        {
            isGameOver = true; // Set flag immediately to prevent re-entry
            Debug.Log($"[GameManager] Player vehicle destroyed - GAME OVER");
            
            // Log game over event
            RaceHistory.Log(
                Assets.Scripts.Logging.EventType.Destruction,
                EventImportance.Critical,
                $"<color=#FF0000><b>GAME OVER</b></color> - {playerVehicle.vehicleName} has been destroyed!",
                playerVehicle.currentStage,
                playerVehicle
            );
            
            // Transition to game over (stops state machine loop)
            stateMachine.TransitionTo(TurnPhase.GameOver);
            
            RefreshAllPanels();
        }
    }

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
        playerVehicle = vehicles.Find(v => v.controlType == ControlType.Player);

        InitializeVehiclePositions(vehicles);
        InitializeControllers(vehicles);
        
        // Start the game - state machine will fire RoundStart
        ProcessStateMachine();
    }

    private void InitializeVehiclePositions(List<Vehicle> vehicles)
    {
        foreach (var vehicle in vehicles)
        {
            Stage startStage = entryStage != null ? entryStage : (stages.Count > 0 ? stages[0] : null);
            vehicle.progress = 0;  // INTEGER: D&D-style discrete position
            
            // Set initial stage and position directly (no events during initialization)
            vehicle.currentStage = startStage;
            if (startStage != null)
            {
                Vector3 stagePos = startStage.transform.position;
                vehicle.transform.position = new Vector3(stagePos.x, stagePos.y, vehicle.transform.position.z);
                
                // Add to stage's vehicle list
                if (!startStage.vehiclesInStage.Contains(vehicle))
                {
                    startStage.vehiclesInStage.Add(vehicle);
                }
                
                // Assign to default lane (middle lane)
                if (startStage.lanes != null && startStage.lanes.Count > 0)
                {
                    startStage.AssignVehicleToDefaultLane(vehicle);
                }
            }
        }
    }

    private void InitializeControllers(List<Vehicle> vehicles)
    {
        // Initialize event logger first
        eventLogger = new TurnEventLogger();
        
        // Log initialization events via logger
        eventLogger.LogRaceInitialized(vehicles.Count, stages.Count);
        foreach (var vehicle in vehicles)
        {
            Stage startStage = vehicle.currentStage;
            eventLogger.LogVehiclePlaced(vehicle, startStage);
            eventLogger.LogCrewComposition(vehicle, startStage);
        }
        
        // Initialize state machine and subscribe logger
        stateMachine = new TurnStateMachine();
        eventLogger.SubscribeToStateMachine(stateMachine);
        
        // Subscribe GameManager to events it needs for game logic
        stateMachine.OnRoundStarted += HandleRoundStart;
        stateMachine.OnRoundEnded += HandleRoundEnd;
        stateMachine.OnGameOver += HandleGameOver;
        
        // Initialize state machine (this will fire events that logger catches)
        stateMachine.Initialize(vehicles);
        
        // Log turn order after initialization
        eventLogger.LogTurnOrderEstablished(stateMachine.AllVehicles);
        
        // Initialize turn controller and subscribe logger
        turnController = gameObject.AddComponent<TurnController>();
        turnController.Initialize(new List<Vehicle>(stateMachine.AllVehicles));
        eventLogger.SubscribeToTurnController(turnController);

        // Initialize player controller and subscribe logger
        playerController = GetComponent<PlayerController>();
        if (playerController == null)
        {
            Debug.LogError("PlayerController component not found!");
            return;
        }
        playerController.Initialize(playerVehicle, turnController, this, OnPlayerTurnComplete);
        eventLogger.SubscribeToPlayerController(playerController);
    }

    // ==================== STATE MACHINE PROCESSING ====================

    /// <summary>
    /// Main state machine driver. Processes phases until hitting a wait state (player input).
    /// </summary>
    private void ProcessStateMachine()
    {
        while (stateMachine.IsActive && !stateMachine.IsWaitingForPlayer && !isGameOver)
        {
            switch (stateMachine.CurrentPhase)
            {
                case TurnPhase.RoundStart:
                    stateMachine.TransitionTo(TurnPhase.TurnStart);
                    break;
                    
                case TurnPhase.TurnStart:
                    ProcessTurnStart();
                    break;
                    
                case TurnPhase.AIAction:
                    ProcessAIAction();
                    break;
                    
                case TurnPhase.TurnEnd:
                    ProcessTurnEnd();
                    break;
                    
                case TurnPhase.RoundEnd:
                    stateMachine.TransitionTo(TurnPhase.RoundStart);
                    break;
                    
                case TurnPhase.GameOver:
                    return;
                    
                default:
                    Debug.LogError($"[GameManager] Unexpected phase: {stateMachine.CurrentPhase}");
                    return;
            }
        }
        
        if (stateMachine.IsWaitingForPlayer && !isGameOver)
        {
            RefreshAllPanels();
        }
    }

    // ==================== PHASE HANDLERS ====================

    private void ProcessTurnStart()
    {
        var vehicle = stateMachine.CurrentVehicle;
        
        if (stateMachine.ShouldSkipTurn(vehicle))
        {
            AdvanceToNextTurn();
            return;
        }
        
        turnController.StartTurn(vehicle);
        
        if (vehicle == playerVehicle)
        {
            stateMachine.TransitionTo(TurnPhase.PlayerAction);
            playerController.ProcessPlayerMovement();
        }
        else
        {
            stateMachine.TransitionTo(TurnPhase.AIAction);
        }
    }

    private void ProcessAIAction()
    {
        var vehicle = stateMachine.CurrentVehicle;
        
        if (vehicle.IsOperational())
        {
            turnController.ExecuteMovement(vehicle);
            HandleStageTransitions(vehicle);
        }
        
        stateMachine.TransitionTo(TurnPhase.TurnEnd);
    }

    private void ProcessTurnEnd()
    {
        var vehicle = stateMachine.CurrentVehicle;
        turnController.EndTurn(vehicle);
        AdvanceToNextTurn();
        RefreshAllPanels();
    }

    private void AdvanceToNextTurn()
    {
        bool newRound = stateMachine.AdvanceToNextTurn();
        
        if (newRound)
        {
            stateMachine.TransitionTo(TurnPhase.RoundEnd);
            RaceHistory.AdvanceTurn();
        }
        else
        {
            stateMachine.TransitionTo(TurnPhase.TurnStart);
        }
    }

    // ==================== EVENT HANDLERS ====================

    private void HandleRoundStart(int roundNumber)
    {
        // Future: Apply round-start effects to all vehicles
    }

    private void HandleRoundEnd(int roundNumber)
    {
        // Future: Apply round-end effects to all vehicles
    }

    private void HandleGameOver()
    {
        Debug.Log("[GameManager] Game Over - simulation stopped");
        
        // Stop any ongoing UI updates
        if (statusNotesText != null)
        {
            statusNotesText.text = "<color=#FF0000><b>GAME OVER</b></color>\nYour vehicle has been destroyed!";
        }
        
        RefreshAllPanels();
    }

    // ==================== PLAYER TURN ====================

    private void OnPlayerTurnComplete()
    {
        stateMachine.TransitionTo(TurnPhase.TurnEnd);
        ProcessStateMachine();
    }

    public bool TriggerPlayerMovement()
    {
        if (playerVehicle == null || !stateMachine.IsWaitingForPlayer) return false;
        
        bool success = turnController.ExecuteMovement(playerVehicle);
        
        if (success)
        {
            HandleStageTransitions(playerVehicle);
            
            // After movement, re-trigger stage processing for crossroads
            // (ProcessPlayerMovement handles the choice UI)
            if (playerController != null)
            {
                playerController.ProcessPlayerMovement();
            }
            
            RefreshAllPanels();
        }
        
        return success;
    }

    // ==================== STAGE TRANSITIONS ====================

    private void HandleStageTransitions(Vehicle vehicle)
    {
        if (vehicle == null || vehicle.currentStage == null) return;
        
        while (vehicle.progress >= vehicle.currentStage.length && vehicle.currentStage.nextStages.Count > 0)
        {
            if (vehicle.currentStage.nextStages.Count == 1)
            {
                // Single path - always take it
                turnController.MoveToStage(vehicle, vehicle.currentStage.nextStages[0]);
            }
            else
            {
                // Crossroads - handle based on control type
                if (vehicle.controlType == ControlType.Player)
                {
                    return; // Player chooses via UI (PlayerController handles)
                }
                else
                {
                    // AI chooses randomly (TODO: Add strategic AI decision-making later)
                    Stage chosenStage = vehicle.currentStage.nextStages[Random.Range(0, vehicle.currentStage.nextStages.Count)];
                    turnController.MoveToStage(vehicle, chosenStage);
                }
            }
        }
    }

    // ==================== UI ====================

    public void RefreshAllPanels()
    {
        if (vehicleInspectorPanel != null)
            vehicleInspectorPanel.OnTurnChanged();
        
        if (overviewPanel != null)
            overviewPanel.RefreshPanel();
      
        if (focusPanel != null)
            focusPanel.RefreshPanel();
    }

    // ==================== PUBLIC API ====================

    public List<Vehicle> GetVehicles()
    {
        if (stateMachine == null) return new List<Vehicle>();
        return new List<Vehicle>(stateMachine.AllVehicles);
    }
    
    public TurnStateMachine GetStateMachine() => stateMachine;
    public TurnController GetTurnController() => turnController;
}