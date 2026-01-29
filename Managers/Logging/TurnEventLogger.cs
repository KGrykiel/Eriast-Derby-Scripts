using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Logging;
using EventType = Assets.Scripts.Logging.EventType;

namespace Assets.Scripts.Managers.Logging
{
    /// <summary>
    /// Subscribes to TurnStateMachine, TurnController, and PlayerController events and logs to RaceHistory.
    /// Keeps logging concerns completely separate from game logic.
    /// 
    /// Usage: Create instance and call SubscribeTo*() methods during initialization.
    /// </summary>
    public class TurnEventLogger
    {
        private TurnStateMachine stateMachine;
        private TurnController turnController;
        private PlayerController playerController;
        
        // ==================== SUBSCRIPTION ====================
        
        /// <summary>
        /// Subscribe to all events from the state machine.
        /// </summary>
        public void SubscribeToStateMachine(TurnStateMachine machine)
        {
            stateMachine = machine;
            
            stateMachine.OnPhaseChanged += LogPhaseChange;
            stateMachine.OnRoundStarted += LogRoundStart;
            stateMachine.OnRoundEnded += LogRoundEnd;
            stateMachine.OnTurnStarted += LogTurnStart;
            stateMachine.OnTurnEnded += LogTurnEnd;
            stateMachine.OnGameOver += LogGameOver;
            stateMachine.OnInitiativeRolled += LogInitiativeRoll;
            stateMachine.OnVehicleRemoved += LogVehicleRemoved;
        }
        
        /// <summary>
        /// Subscribe to all events from the turn controller.
        /// </summary>
        public void SubscribeToTurnController(TurnController controller)
        {
            turnController = controller;
            
            turnController.OnAutoMovement += LogAutoMovement;
            turnController.OnComponentPowerShutdown += LogComponentPowerShutdown;
            turnController.OnMovementBlocked += LogMovementBlocked;
            turnController.OnMovementExecuted += LogMovementExecuted;
            turnController.OnStageEntered += LogStageEntered;
        }
        
        /// <summary>
        /// Subscribe to all events from the player controller.
        /// </summary>
        public void SubscribeToPlayerController(PlayerController controller)
        {
            playerController = controller;
            
            playerController.OnPlayerCannotAct += LogPlayerCannotAct;
            playerController.OnPlayerActionPhaseStarted += LogPlayerActionPhaseStarted;
            playerController.OnPlayerEndedTurn += LogPlayerEndedTurn;
            playerController.OnPlayerTriggeredMovement += LogPlayerTriggeredMovement;
        }
        
        /// <summary>
        /// Unsubscribe from all events (call on cleanup).
        /// </summary>
        public void Unsubscribe()
        {
            if (stateMachine != null)
            {
                stateMachine.OnPhaseChanged -= LogPhaseChange;
                stateMachine.OnRoundStarted -= LogRoundStart;
                stateMachine.OnRoundEnded -= LogRoundEnd;
                stateMachine.OnTurnStarted -= LogTurnStart;
                stateMachine.OnTurnEnded -= LogTurnEnd;
                stateMachine.OnGameOver -= LogGameOver;
                stateMachine.OnInitiativeRolled -= LogInitiativeRoll;
                stateMachine.OnVehicleRemoved -= LogVehicleRemoved;
            }
            
            if (turnController != null)
            {
                turnController.OnAutoMovement -= LogAutoMovement;
                turnController.OnComponentPowerShutdown -= LogComponentPowerShutdown;
                turnController.OnMovementBlocked -= LogMovementBlocked;
                turnController.OnMovementExecuted -= LogMovementExecuted;
                turnController.OnStageEntered -= LogStageEntered;
            }
            
            if (playerController != null)
            {
                playerController.OnPlayerCannotAct -= LogPlayerCannotAct;
                playerController.OnPlayerActionPhaseStarted -= LogPlayerActionPhaseStarted;
                playerController.OnPlayerEndedTurn -= LogPlayerEndedTurn;
                playerController.OnPlayerTriggeredMovement -= LogPlayerTriggeredMovement;
            }
        }
        
        // ==================== STATE MACHINE EVENT HANDLERS ====================
        
        
        private void LogPhaseChange(TurnPhase oldPhase, TurnPhase newPhase)
        {
            // Only log significant phase changes (not every transition)
            // Most logging happens in specific event handlers
        }
        
        private void LogRoundStart(int roundNumber)
        {
            RaceHistory.Log(
                EventType.System,
                EventImportance.Medium,
                $"═══════════ Round {roundNumber} Begins ═══════════"
            ).WithMetadata("round", roundNumber)
             .WithMetadata("vehicleCount", stateMachine.AllVehicles.Count);
        }
        
        private void LogRoundEnd(int roundNumber)
        {
            RaceHistory.Log(
                EventType.System,
                EventImportance.Low,
                $"═══════════ Round {roundNumber} Ends ═══════════"
            ).WithMetadata("round", roundNumber);
        }
        
        private void LogTurnStart(Vehicle vehicle)
        {
            RaceHistory.Log(
                EventType.System,
                EventImportance.Low,
                $"{vehicle.vehicleName}'s turn begins (Turn {stateMachine.CurrentTurnIndex + 1}/{stateMachine.AllVehicles.Count})",
                vehicle.currentStage,
                vehicle
            ).WithMetadata("turnIndex", stateMachine.CurrentTurnIndex)
             .WithMetadata("round", stateMachine.CurrentRound);
        }
        
        private void LogTurnEnd(Vehicle vehicle)
        {
            RaceHistory.Log(
                EventType.System,
                EventImportance.Low,
                $"{vehicle.vehicleName}'s turn ends",
                vehicle.currentStage,
                vehicle
            );
        }
        
        private void LogGameOver()
        {
            RaceHistory.Log(
                EventType.System,
                EventImportance.Critical,
                "Race ended!"
            );
        }
        
        private void LogInitiativeRoll(Vehicle vehicle, int initiative)
        {
            RaceHistory.Log(
                EventType.System,
                EventImportance.Low,
                $"{vehicle.vehicleName} rolled initiative: {initiative}",
                null,
                vehicle
            ).WithMetadata("initiative", initiative);
        }
        
        private void LogVehicleRemoved(Vehicle vehicle)
        {
            RaceHistory.Log(
                EventType.System,
                EventImportance.High,
                $"{vehicle.vehicleName} removed from turn order",
                vehicle.currentStage,
                vehicle
            );
        }
        
        // ==================== TURN CONTROLLER EVENT HANDLERS ====================
        
        private void LogAutoMovement(Vehicle vehicle)
        {
            RaceHistory.Log(
                EventType.Movement,
                EventImportance.Low,
                $"{vehicle.vehicleName} automatically moved (movement not triggered manually)",
                vehicle.currentStage,
                vehicle
            ).WithMetadata("automatic", true);
        }
        
        private void LogComponentPowerShutdown(Vehicle vehicle, VehicleComponent component, int required, int available)
        {
            RaceHistory.Log(
                EventType.Resource,
                EventImportance.Medium,
                $"{vehicle.vehicleName}: {component.name} shut down (needs {required}, have {available})",
                vehicle.currentStage,
                vehicle
            ).WithMetadata("component", component.name)
             .WithMetadata("requiredPower", required)
             .WithMetadata("availablePower", available);
        }
        
        private void LogMovementBlocked(Vehicle vehicle, string reason)
        {
            RaceHistory.Log(
                EventType.Movement,
                EventImportance.Medium,
                $"{vehicle.vehicleName} cannot move: {reason}",
                vehicle.currentStage,
                vehicle
            ).WithMetadata("reason", reason);
        }
        
        private void LogMovementExecuted(Vehicle vehicle, int distance, int speed, int oldProgress, int newProgress)
        {
            RaceHistory.Log(
                EventType.Movement,
                EventImportance.Low,
                $"{vehicle.vehicleName} moved {distance} units (speed {speed})",
                vehicle.currentStage,
                vehicle
            ).WithMetadata("distance", distance)
             .WithMetadata("speed", speed)
             .WithMetadata("oldProgress", oldProgress)
             .WithMetadata("newProgress", newProgress);
        }
        
        private void LogStageEntered(Vehicle vehicle, Stage newStage, Stage previousStage, int carriedProgress, bool isPlayerChoice)
        {
            EventImportance importance = isPlayerChoice ? EventImportance.Medium : EventImportance.Low;
            
            RaceHistory.Log(
                EventType.Movement,
                importance,
                $"{vehicle.vehicleName} entered {newStage.stageName}",
                newStage,
                vehicle
            ).WithMetadata("previousStage", previousStage != null ? previousStage.stageName : null ?? "None")
             .WithMetadata("carriedProgress", carriedProgress)
             .WithMetadata("isPlayerChoice", isPlayerChoice);
        }
        
        // ==================== PLAYER CONTROLLER EVENT HANDLERS ====================
        
        private void LogPlayerCannotAct(Vehicle vehicle, string reason)
        {
            RaceHistory.Log(
                EventType.System,
                EventImportance.High,
                $"{vehicle.vehicleName} cannot act: {reason}",
                vehicle.currentStage,
                vehicle
            ).WithMetadata("nonOperational", true)
             .WithMetadata("reason", reason);
        }
        
        private void LogPlayerActionPhaseStarted(Vehicle vehicle)
        {
            RaceHistory.Log(
                EventType.System,
                EventImportance.Medium,
                $"{vehicle.vehicleName} can now take actions",
                vehicle.currentStage,
                vehicle
            );
        }
        
        private void LogPlayerEndedTurn(Vehicle vehicle)
        {
            RaceHistory.Log(
                EventType.System,
                EventImportance.Low,
                $"{vehicle.vehicleName} ended turn",
                vehicle.currentStage,
                vehicle
            );
        }
        
        private void LogPlayerTriggeredMovement(Vehicle vehicle)
        {
            RaceHistory.Log(
                EventType.Movement,
                EventImportance.Low,
                $"{vehicle.vehicleName} moved forward (player triggered)",
                vehicle.currentStage,
                vehicle
            ).WithMetadata("manual", true);
        }
        
        // ==================== INITIALIZATION LOGGING ====================
        
        
        /// <summary>
        /// Log race initialization (called by GameManager).
        /// </summary>
        public void LogRaceInitialized(int vehicleCount, int stageCount)
        {
            RaceHistory.Log(
                EventType.System,
                EventImportance.High,
                $"Race initialized with {vehicleCount} vehicles and {stageCount} stages"
            );
        }
        
        /// <summary>
        /// Log turn order after initiative is rolled.
        /// </summary>
        public void LogTurnOrderEstablished(IReadOnlyList<Vehicle> vehicles)
        {
            string turnOrder = string.Join(", ", vehicles.Select(v => v.vehicleName));
            RaceHistory.Log(
                EventType.System,
                EventImportance.Medium,
                $"Turn order established: {turnOrder}"
            );
        }
        
        /// <summary>
        /// Log vehicle starting position.
        /// </summary>
        public void LogVehiclePlaced(Vehicle vehicle, Stage stage)
        {
            RaceHistory.Log(
                EventType.System,
                EventImportance.Low,
                $"{vehicle.vehicleName} placed at starting position",
                stage,
                vehicle
            );
        }
        
        /// <summary>
        /// Log crew composition for a vehicle.
        /// </summary>
        public void LogCrewComposition(Vehicle vehicle, Stage stage)
        {
            if (vehicle.seats.Count == 0) return;
            
            var crewList = vehicle.seats
                .Where(s => s != null && s.assignedCharacter != null)
                .Select(s => $"{s.assignedCharacter.characterName} ({s.seatName})")
                .ToList();
            
            if (crewList.Count > 0)
            {
                RaceHistory.Log(
                    EventType.System,
                    EventImportance.Medium,
                    $"{vehicle.vehicleName} crew: {string.Join(", ", crewList)}",
                    stage,
                    vehicle
                ).WithMetadata("crewCount", crewList.Count)
                 .WithMetadata("seatCount", vehicle.seats.Count);
            }
        }
    }
}
