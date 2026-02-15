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
/// Main game coordinator (Facade).
/// Initializes game, starts state machine, handles player turn callback and game over.
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

        phaseContext = new TurnPhaseContext(stateMachine, turnController, playerController);
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
        eventLogger = new TurnEventLogger();
        eventLogger.LogRaceInitialized(vehicles.Count, stages.Count);

        foreach (var vehicle in vehicles)
        {
            Stage startStage = vehicle.currentStage;
            eventLogger.LogVehiclePlaced(vehicle, startStage);
            eventLogger.LogCrewComposition(vehicle, startStage);
        }

        stateMachine = new TurnStateMachine();
        stateMachine.Initialize(vehicles);

        eventLogger.SetStateMachineReference(stateMachine);
        eventLogger.SubscribeToTurnEventBus();

        TurnEventBus.OnGameOver += HandleGameOver;
        TurnEventBus.OnVehicleDestroyed += CheckGameOverCondition;

        eventLogger.LogTurnOrderEstablished(stateMachine.AllVehicles);

        turnController = new TurnService(new List<Vehicle>(stateMachine.AllVehicles));

        playerController = GetComponent<PlayerController>();
        if (playerController == null)
        {
            Debug.LogError("PlayerController component not found!");
            return;
        }
        playerController.Initialize(turnController, stateMachine, OnPlayerTurnComplete);
    }

    // ==================== PLAYER TURN CALLBACK ====================

    private void OnPlayerTurnComplete()
    {
        stateMachine.Resume(phaseContext, TurnPhase.TurnEnd);
    }

    // ==================== EVENT HANDLERS ====================

    /// <summary>Triggers game over when all player vehicles are destroyed.</summary>
    private void CheckGameOverCondition(Vehicle destroyedVehicle)
    {
        if (destroyedVehicle == null || isGameOver) return;
        if (destroyedVehicle.controlType != ControlType.Player) return;

        bool anyPlayerVehiclesRemain = stateMachine.AllVehicles
            .Any(v => v.controlType == ControlType.Player && v.Status != VehicleStatus.Destroyed);

        if (!anyPlayerVehiclesRemain)
        {
            isGameOver = true;
            if (phaseContext != null)
                phaseContext.IsGameOver = true;

            stateMachine.TransitionTo(TurnPhase.GameOver);
        }
    }

    private void HandleGameOver()
    {
        if (statusNotesText != null)
            statusNotesText.text = "<color=#FF0000><b>GAME OVER</b></color>\nAll player vehicles have been destroyed!";
    }

    // ==================== PUBLIC API ====================

    public List<Vehicle> GetVehicles()
    {
        if (stateMachine == null) return new List<Vehicle>();
        return new List<Vehicle>(stateMachine.AllVehicles);
    }
    
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