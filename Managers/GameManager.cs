using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using Assets.Scripts.Managers.Logging;
using Assets.Scripts.Managers.TurnPhases;
using Assets.Scripts.Logging;
using Assets.Scripts.Stages;
using Assets.Scripts.Entities.Vehicles;
using Assets.Scripts.Managers.Logging.Results;

namespace Assets.Scripts.Managers
{
    [RequireComponent(typeof(PlayerController))]

    /// <summary>
    /// Main game coordinator (Facade).
    /// Initializes game, starts state machine, handles player turn callback and game over.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        [Header("Course Layout")]
        [Tooltip("Defines stage connections and lane routing for this course.")]
        [SerializeField] private TrackDefinition trackDefinition;

        [Header("Race Settings")]
        [Tooltip("Maximum number of rounds before the race ends. Vehicles still active at this point are classified as DNF.")]
        [SerializeField] private int maxRounds = 100;

        [Header("Game Over UI")]
        [Tooltip("Optional - status text for game over message")]
        [SerializeField] private TextMeshProUGUI statusNotesText;

        // Controllers
        private TurnStateMachine stateMachine;
        private TurnService turnController;
        private PlayerController playerController;
        private TurnEventLogger eventLogger;
        private RaceCompletionTracker raceTracker;

        // Phase context (passed to handlers)
        private TurnPhaseContext phaseContext;

        // ==================== INITIALIZATION ====================

        void Start()
        {
            InitializeGame();
        }

        private void InitializeGame()
        {
            if (trackDefinition != null)
                trackDefinition.SetAsActive();
            else
                Debug.LogWarning("[GameManager] No TrackDefinition assigned — stage transitions will not work.");

            RaceHistory.ClearHistory();

            List<Vehicle> vehicles = RacePositionTracker.GetAll();

            InitializeVehiclePositions(vehicles);
            InitializeControllers(vehicles);
        }

        private void InitializeVehiclePositions(List<Vehicle> vehicles)
        {
            if (TrackDefinition.Active == null) return;
            Stage startStage = TrackDefinition.Active.GetEntryStage();
            if (startStage == null) return;

            RaceInitialiser.PlaceVehicles(startStage, vehicles);

            Vector3 stagePos = startStage.transform.position;
            foreach (var vehicle in vehicles)
                vehicle.transform.position = new Vector3(stagePos.x, stagePos.y, vehicle.transform.position.z);
        }

        private void InitializeControllers(List<Vehicle> vehicles)
        {
            eventLogger = new TurnEventLogger();
            eventLogger.LogRaceInitialized(vehicles.Count, TrackDefinition.GetAll().Count);

            foreach (var vehicle in vehicles)
            {
                Stage startStage = RacePositionTracker.GetStage(vehicle);
                eventLogger.LogVehiclePlaced(vehicle, startStage);
                eventLogger.LogCrewComposition(vehicle, startStage);
            }

            stateMachine = new TurnStateMachine();
            stateMachine.Initialize(vehicles);

            eventLogger.SetStateMachineReference(stateMachine);
            eventLogger.SubscribeToTurnEventBus();

            TurnEventBus.OnEvent += HandleTurnEvent;

            raceTracker = new RaceCompletionTracker(stateMachine, vehicles, maxRounds);
            raceTracker.Subscribe();

            eventLogger.LogTurnOrderEstablished(stateMachine.AllVehicles);

            turnController = new TurnService(new List<Vehicle>(stateMachine.AllVehicles));

            playerController = GetComponent<PlayerController>();
            if (playerController == null)
            {
                Debug.LogError("PlayerController component not found!");
                return;
            }
            playerController.Initialize(turnController);

            phaseContext = new TurnPhaseContext(stateMachine, turnController, playerController, new VehicleActionManager());
            phaseContext.RegisterController(ControlType.Player, new PlayerTurnController(playerController.InputCoordinator));
            phaseContext.RegisterController(ControlType.AI, new AITurnController());

            RaceHistory.Initialize(stateMachine);

            stateMachine.Run(phaseContext);
        }

        // ==================== EVENT HANDLERS ====================

        private void HandleTurnEvent(TurnEvent evt)
        {
            if (evt is RaceOverEvent raceOver)
                HandleRaceOver(raceOver.Result);
        }

        private void HandleRaceOver(RaceResult result)
        {
            if (phaseContext != null)
                phaseContext.IsRaceOver = true;

            if (statusNotesText != null)
            {
                string winnerName = result.Winner != null ? result.Winner.vehicleName : "Nobody";
                statusNotesText.text = $"<color=#FFD700><b>RACE COMPLETE!</b></color>\n{winnerName} wins!";
            }
        }

        // ==================== CLEANUP ====================

        void OnDestroy()
        {
            TurnEventBus.OnEvent -= HandleTurnEvent;
            raceTracker?.Unsubscribe();
            eventLogger?.Unsubscribe();
            stateMachine?.Cleanup();
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
}