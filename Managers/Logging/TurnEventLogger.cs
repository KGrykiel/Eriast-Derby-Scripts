using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Entities.Vehicles;
using Assets.Scripts.Entities.Vehicles.VehicleComponents;
using Assets.Scripts.Logging;
using Assets.Scripts.Managers.Logging.Results;
using Assets.Scripts.Stages;
using EventType = Assets.Scripts.Logging.EventType;

namespace Assets.Scripts.Managers.Logging
{
    /// <summary>Subscribes to TurnEventBus and logs all turn events to RaceHistory.</summary>
    public class TurnEventLogger
    {
        private TurnStateMachine stateMachine;

        public void SetStateMachineReference(TurnStateMachine machine)
        {
            stateMachine = machine;
        }

        public void SubscribeToTurnEventBus()
        {
            TurnEventBus.OnEvent += HandleEvent;
        }

        public void Unsubscribe()
        {
            TurnEventBus.OnEvent -= HandleEvent;
        }

        private void HandleEvent(TurnEvent evt)
        {
            switch (evt)
            {
                case RoundStartedEvent e:               LogRoundStart(e.RoundNumber); break;
                case RoundEndedEvent e:                 LogRoundEnd(e.RoundNumber); break;
                case TurnStartedEvent e:                LogTurnStart(e.Vehicle); break;
                case TurnEndedEvent e:                  LogTurnEnd(e.Vehicle); break;
                case RaceOverEvent e:                   LogRaceOver(e.Result); break;
                case InitiativeRolledEvent e:           LogInitiativeRoll(e.Vehicle, e.Initiative); break;
                case VehicleRemovedEvent e:             LogVehicleRemoved(e.Vehicle); break;
                case VehicleDestroyedEvent e:           LogVehicleDestroyed(e.Vehicle); break;
                case VehicleFinishedEvent e:            LogVehicleFinished(e.Vehicle); break;
                case AutoMovementEvent e:               LogAutoMovement(e.Vehicle); break;
                case ComponentPowerShutdownEvent e:     LogComponentPowerShutdown(e.Vehicle, e.Component, e.RequiredPower, e.AvailablePower); break;
                case MovementBlockedEvent e:            LogMovementBlocked(e.Vehicle, e.Reason); break;
                case MovementExecutedEvent e:           LogMovementExecuted(e.Vehicle, e.Distance, e.Speed); break;
                case StageEnteredEvent e:               LogStageEntered(e.Vehicle, e.NewStage); break;
                case FinishLineCrossedEvent e:          LogFinishLineCrossed(e.Vehicle, e.FinishStage); break;
                case PlayerCannotActEvent e:            LogPlayerCannotAct(e.Vehicle, e.Reason); break;
                case PlayerActionPhaseStartedEvent e:   LogPlayerActionPhaseStarted(e.Vehicle); break;
                case PlayerEndedTurnEvent e:            LogPlayerEndedTurn(e.Vehicle); break;
                case PlayerTriggeredMovementEvent e:    LogPlayerTriggeredMovement(e.Vehicle); break;
            }
        }
        
        // ==================== LIFECYCLE EVENT HANDLERS ====================
        
        private void LogRoundStart(int roundNumber)
        {
            RaceHistory.Log(
                EventType.System,
                EventImportance.Medium,
                $"----------- Round {LogColors.Number(roundNumber.ToString())} Begins -----------"
            );
        }
        
        private void LogRoundEnd(int roundNumber)
        {
            RaceHistory.Log(
                EventType.System,
                EventImportance.Low,
                $"----------- Round {LogColors.Number(roundNumber.ToString())} Ends -----------"
            );
        }
        
        private void LogTurnStart(Vehicle vehicle)
        {
            int turnIndex = stateMachine?.CurrentTurnIndex ?? 0;
            int totalVehicles = stateMachine?.AllVehicles.Count ?? 0;

            RaceHistory.Log(
                EventType.System,
                EventImportance.Low,
                $"{LogColors.Vehicle(vehicle.vehicleName)}'s turn begins (Turn {LogColors.Number($"{turnIndex + 1}/{totalVehicles}")})",
                RacePositionTracker.GetStage(vehicle),
                vehicle
            );
        }
        
        private void LogTurnEnd(Vehicle vehicle)
        {
            RaceHistory.Log(
                EventType.System,
                EventImportance.Low,
                $"{LogColors.Vehicle(vehicle.vehicleName)}'s turn ends",
                RacePositionTracker.GetStage(vehicle),
                vehicle
            );
        }
        
        private void LogRaceOver(RaceResult result)
        {
            string winnerName = result.Winner != null
                ? $"{LogColors.Vehicle(result.Winner.vehicleName)}"
                : "nobody";
            RaceHistory.Log(
                EventType.FinishLine,
                EventImportance.Critical,
                $"RACE COMPLETE - {winnerName} wins! ({LogColors.Number(result.TotalParticipants.ToString())} starters, {LogColors.Number(result.TotalRounds.ToString())} rounds)"
            );

            foreach (var record in result.Finishers)
            {
                RaceHistory.Log(
                    EventType.FinishLine,
                    EventImportance.High,
                    $"  #{LogColors.Number(record.Position.ToString())} {LogColors.Vehicle(record.Vehicle.vehicleName)} - finished Round {LogColors.Number(record.Round.ToString())}",
                    null,
                    record.Vehicle
                );
            }

            foreach (var record in result.DidNotFinish)
            {
                string stageName = record.EliminatedAt != null ? record.EliminatedAt.stageName : "unknown";
                RaceHistory.Log(
                    EventType.FinishLine,
                    EventImportance.High,
                    $"  DNF {LogColors.Vehicle(record.Vehicle.vehicleName)} - eliminated Round {LogColors.Number(record.Round.ToString())} at {stageName} (progress {LogColors.Number(record.ProgressAtElimination.ToString())})",
                    record.EliminatedAt,
                    record.Vehicle
                );
            }
        }
        
        private void LogInitiativeRoll(Vehicle vehicle, int initiative)
        {
            RaceHistory.Log(
                EventType.System,
                EventImportance.Low,
                $"{LogColors.Vehicle(vehicle.vehicleName)} rolled initiative: {LogColors.Number(initiative.ToString())}",
                null,
                vehicle
            );
        }
        
        private void LogVehicleRemoved(Vehicle vehicle)
        {
            RaceHistory.Log(
                EventType.System,
                EventImportance.High,
                $"{LogColors.Vehicle(vehicle.vehicleName)} removed from turn order",
                RacePositionTracker.GetStage(vehicle),
                vehicle
            );
        }
        
        private void LogVehicleDestroyed(Vehicle vehicle)
        {
            RaceHistory.Log(
                EventType.Destruction,
                EventImportance.Critical,
                $"{LogColors.Vehicle(vehicle.vehicleName)} has been destroyed!",
                RacePositionTracker.GetStage(vehicle),
                vehicle
            );
        }

        private void LogVehicleFinished(Vehicle vehicle)
        {
            RaceHistory.Log(
                EventType.FinishLine,
                EventImportance.Critical,
                $"{LogColors.Vehicle(vehicle.vehicleName)} has finished the race!",
                RacePositionTracker.GetStage(vehicle),
                vehicle
            );
        }
        
        // ==================== OPERATION EVENT HANDLERS ====================
        
        private void LogAutoMovement(Vehicle vehicle)
        {
            RaceHistory.Log(
                EventType.Movement,
                EventImportance.Low,
                $"{LogColors.Vehicle(vehicle.vehicleName)} automatically moved (movement not triggered manually)",
                RacePositionTracker.GetStage(vehicle),
                vehicle
            );
        }
        
        private void LogComponentPowerShutdown(Vehicle vehicle, VehicleComponent component, int required, int available)
        {
            RaceHistory.Log(
                EventType.Resource,
                EventImportance.Medium,
                $"{LogColors.Vehicle(vehicle.vehicleName)}: {component.name} shut down (needs {LogColors.Energy($"{required} energy")}, have {LogColors.Energy($"{available} energy")})",
                RacePositionTracker.GetStage(vehicle),
                vehicle
            );
        }
        
        private void LogMovementBlocked(Vehicle vehicle, string reason)
        {
            RaceHistory.Log(
                EventType.Movement,
                EventImportance.Medium,
                $"{LogColors.Vehicle(vehicle.vehicleName)} cannot move: {reason}",
                RacePositionTracker.GetStage(vehicle),
                vehicle
            );
        }
        
        private void LogMovementExecuted(Vehicle vehicle, int distance, int speed)
        {
            RaceHistory.Log(
                EventType.Movement,
                EventImportance.Low,
                $"{LogColors.Vehicle(vehicle.vehicleName)} moved {LogColors.Number($"{distance} units")} (speed {LogColors.Number(speed.ToString())})",
                RacePositionTracker.GetStage(vehicle),
                vehicle
            );
        }
        
        private void LogStageEntered(Vehicle vehicle, Stage newStage)
        {
            RaceHistory.Log(
                EventType.Movement,
                EventImportance.Low,
                $"{LogColors.Vehicle(vehicle.vehicleName)} entered {newStage.stageName}",
                newStage,
                vehicle
            );
        }
        
        private void LogFinishLineCrossed(Vehicle vehicle, Stage finishStage)
        {
            RaceHistory.Log(
                EventType.FinishLine,
                EventImportance.Critical,
                $"[FINISH] {LogColors.Vehicle(vehicle.vehicleName)} crossed the finish line!",
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
                $"{LogColors.Vehicle(vehicle.vehicleName)} cannot act: {reason}",
                RacePositionTracker.GetStage(vehicle),
                vehicle
            );
        }
        
        private void LogPlayerActionPhaseStarted(Vehicle vehicle)
        {
            RaceHistory.Log(
                EventType.System,
                EventImportance.Medium,
                $"{LogColors.Vehicle(vehicle.vehicleName)} can now take actions",
                RacePositionTracker.GetStage(vehicle),
                vehicle
            );
        }
        
        private void LogPlayerEndedTurn(Vehicle vehicle)
        {
            RaceHistory.Log(
                EventType.System,
                EventImportance.Low,
                $"{LogColors.Vehicle(vehicle.vehicleName)} ended turn",
                RacePositionTracker.GetStage(vehicle),
                vehicle
            );
        }
        
        private void LogPlayerTriggeredMovement(Vehicle vehicle)
        {
            RaceHistory.Log(
                EventType.Movement,
                EventImportance.Low,
                $"{LogColors.Vehicle(vehicle.vehicleName)} moved forward (player triggered)",
                RacePositionTracker.GetStage(vehicle),
                vehicle
            );
        }
        
        // ==================== INITIALIZATION LOGGING ====================

        public void LogRaceInitialized(int vehicleCount, int stageCount)
        {
            RaceHistory.Log(
                EventType.System,
                EventImportance.High,
                $"Race initialized with {LogColors.Number(vehicleCount.ToString())} vehicles and {LogColors.Number(stageCount.ToString())} stages"
            );
        }
        
        public void LogTurnOrderEstablished(IReadOnlyList<Vehicle> vehicles)
        {
            string turnOrder = string.Join(", ", vehicles.Select(v => $"{LogColors.Vehicle(v.vehicleName)}"));
            RaceHistory.Log(
                EventType.System,
                EventImportance.Medium,
                $"Turn order established: {turnOrder}"
            );
        }
        
        public void LogVehiclePlaced(Vehicle vehicle, Stage stage)
        {
            RaceHistory.Log(
                EventType.System,
                EventImportance.Low,
                $"{LogColors.Vehicle(vehicle.vehicleName)} placed at starting position",
                stage,
                vehicle
            );
        }
        
        public void LogCrewComposition(Vehicle vehicle, Stage stage)
        {
            if (vehicle.seats.Count == 0) return;
            
            var crewList = vehicle.seats
                .Where(s => s != null && s.IsAssigned)
                .Select(s => $"{s.GetDisplayName()} ({s.seatName})")
                .ToList();
            
            if (crewList.Count > 0)
            {
                RaceHistory.Log(
                    EventType.System,
                    EventImportance.Medium,
                    $"{LogColors.Vehicle(vehicle.vehicleName)} crew: {string.Join(", ", crewList)}",
                    stage,
                    vehicle
                );
            }
        }
    }
}


