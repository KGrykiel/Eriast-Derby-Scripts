using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Logging;
using Assets.Scripts.Managers;
using Assets.Scripts.Stages;
using EventType = Assets.Scripts.Logging.EventType;

namespace Assets.Scripts.Managers.Logging
{
    /// <summary>
    /// Subscribes to TurnEventBus and logs all turn-related events to RaceHistory.
    /// Keeps logging concerns completely separate from game logic.
    /// 
    /// TurnEventBus is the single source of truth for all turn events:
    /// - Lifecycle events (rounds, turns, game over)
    /// - Operation events (movement, power, stages)
    /// - Player events (player actions)
    /// 
    /// Usage: Create instance and call SubscribeToTurnEventBus() during initialization.
    /// </summary>
    public class TurnEventLogger
    {
        private TurnStateMachine stateMachine;
        
        // ==================== SUBSCRIPTION ====================
        
        /// <summary>
        /// Store reference to state machine for metadata in logs.
        /// </summary>
        public void SetStateMachineReference(TurnStateMachine machine)
        {
            stateMachine = machine;
        }
        
        /// <summary>
        /// Subscribe to TurnEventBus for ALL turn-related events.
        /// Single subscription point for lifecycle, operations, and player events.
        /// </summary>
        public void SubscribeToTurnEventBus()
        {
            // Lifecycle events
            TurnEventBus.OnPhaseChanged += LogPhaseChange;
            TurnEventBus.OnRoundStarted += LogRoundStart;
            TurnEventBus.OnRoundEnded += LogRoundEnd;
            TurnEventBus.OnTurnStarted += LogTurnStart;
            TurnEventBus.OnTurnEnded += LogTurnEnd;
            TurnEventBus.OnGameOver += LogGameOver;
            TurnEventBus.OnInitiativeRolled += LogInitiativeRoll;
            TurnEventBus.OnVehicleRemoved += LogVehicleRemoved;
            TurnEventBus.OnVehicleDestroyed += LogVehicleDestroyed;
            
            // Operation events
            TurnEventBus.OnAutoMovement += LogAutoMovement;
            TurnEventBus.OnComponentPowerShutdown += LogComponentPowerShutdown;
            TurnEventBus.OnMovementBlocked += LogMovementBlocked;
            TurnEventBus.OnMovementExecuted += LogMovementExecuted;
            TurnEventBus.OnStageEntered += LogStageEntered;
            TurnEventBus.OnFinishLineCrossed += LogFinishLineCrossed;
            
            // Player events
            TurnEventBus.OnPlayerCannotAct += LogPlayerCannotAct;
            TurnEventBus.OnPlayerActionPhaseStarted += LogPlayerActionPhaseStarted;
            TurnEventBus.OnPlayerEndedTurn += LogPlayerEndedTurn;
            TurnEventBus.OnPlayerTriggeredMovement += LogPlayerTriggeredMovement;
        }
        
        /// <summary>
        /// Unsubscribe from all events (call on cleanup).
        /// </summary>
        public void Unsubscribe()
        {
            // Lifecycle events
            TurnEventBus.OnPhaseChanged -= LogPhaseChange;
            TurnEventBus.OnRoundStarted -= LogRoundStart;
            TurnEventBus.OnRoundEnded -= LogRoundEnd;
            TurnEventBus.OnTurnStarted -= LogTurnStart;
            TurnEventBus.OnTurnEnded -= LogTurnEnd;
            TurnEventBus.OnGameOver -= LogGameOver;
            TurnEventBus.OnInitiativeRolled -= LogInitiativeRoll;
            TurnEventBus.OnVehicleRemoved -= LogVehicleRemoved;
            TurnEventBus.OnVehicleDestroyed -= LogVehicleDestroyed;
            
            // Operation events
            TurnEventBus.OnAutoMovement -= LogAutoMovement;
            TurnEventBus.OnComponentPowerShutdown -= LogComponentPowerShutdown;
            TurnEventBus.OnMovementBlocked -= LogMovementBlocked;
            TurnEventBus.OnMovementExecuted -= LogMovementExecuted;
            TurnEventBus.OnStageEntered -= LogStageEntered;
            TurnEventBus.OnFinishLineCrossed -= LogFinishLineCrossed;
            
            // Player events
            TurnEventBus.OnPlayerCannotAct -= LogPlayerCannotAct;
            TurnEventBus.OnPlayerActionPhaseStarted -= LogPlayerActionPhaseStarted;
            TurnEventBus.OnPlayerEndedTurn -= LogPlayerEndedTurn;
            TurnEventBus.OnPlayerTriggeredMovement -= LogPlayerTriggeredMovement;
        }
        
        // ==================== LIFECYCLE EVENT HANDLERS ====================
        
        private void LogPhaseChange(TurnPhase oldPhase, TurnPhase newPhase)
        {
            // Only log significant phase changes (not every transition)
            // Most logging happens in specific event handlers
        }
        
        private void LogRoundStart(int roundNumber)
        {
            int vehicleCount = stateMachine?.AllVehicles.Count ?? 0;
            RaceHistory.Log(
                EventType.System,
                EventImportance.Medium,
                $"═══════════ Round {roundNumber} Begins ═══════════"
            ).WithMetadata("round", roundNumber)
             .WithMetadata("vehicleCount", vehicleCount);
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
            int turnIndex = stateMachine?.CurrentTurnIndex ?? 0;
            int totalVehicles = stateMachine?.AllVehicles.Count ?? 0;
            int round = stateMachine?.CurrentRound ?? 0;
            
            RaceHistory.Log(
                EventType.System,
                EventImportance.Low,
                $"{vehicle.vehicleName}'s turn begins (Turn {turnIndex + 1}/{totalVehicles})",
                vehicle.currentStage,
                vehicle
            ).WithMetadata("turnIndex", turnIndex)
             .WithMetadata("round", round);
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
                "<color=#FF0000><b>GAME OVER</b></color> - All player vehicles have been destroyed!"
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
        
        private void LogVehicleDestroyed(Vehicle vehicle)
        {
            RaceHistory.Log(
                EventType.Destruction,
                EventImportance.Critical,
                $"<color=#FF6600>{vehicle.vehicleName} has been destroyed!</color>",
                vehicle.currentStage,
                vehicle
            );
        }
        
        // ==================== OPERATION EVENT HANDLERS ====================
        
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
            string previousStageName = previousStage != null ? previousStage.stageName : "None";
            
            RaceHistory.Log(
                EventType.Movement,
                importance,
                $"{vehicle.vehicleName} entered {newStage.stageName}",
                newStage,
                vehicle
            ).WithMetadata("previousStage", previousStageName)
             .WithMetadata("carriedProgress", carriedProgress)
             .WithMetadata("isPlayerChoice", isPlayerChoice);
        }
        
        private void LogFinishLineCrossed(Vehicle vehicle, Stage finishStage)
        {
            RaceHistory.Log(
                EventType.FinishLine,
                EventImportance.Critical,
                $"[FINISH] {vehicle.vehicleName} crossed the finish line!",
                finishStage,
                vehicle
            );
        }
        
        // ==================== PLAYER EVENT HANDLERS ====================
        
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
