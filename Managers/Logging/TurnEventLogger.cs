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
                case StageEnteredEvent e:               LogStageEntered(e.Vehicle, e.NewStage, e.IsPlayerChoice); break;
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
                $"----------- Round {roundNumber} Begins -----------"
            );
        }
        
        private void LogRoundEnd(int roundNumber)
        {
            RaceHistory.Log(
                EventType.System,
                EventImportance.Low,
                $"----------- Round {roundNumber} Ends -----------"
            );
        }
        
        private void LogTurnStart(Vehicle vehicle)
        {
            int turnIndex = stateMachine?.CurrentTurnIndex ?? 0;
            int totalVehicles = stateMachine?.AllVehicles.Count ?? 0;

            RaceHistory.Log(
                EventType.System,
                EventImportance.Low,
                $"{vehicle.vehicleName}'s turn begins (Turn {turnIndex + 1}/{totalVehicles})",
                RacePositionTracker.GetStage(vehicle),
                vehicle
            );
        }
        
        private void LogTurnEnd(Vehicle vehicle)
        {
            RaceHistory.Log(
                EventType.System,
                EventImportance.Low,
                $"{vehicle.vehicleName}'s turn ends",
                RacePositionTracker.GetStage(vehicle),
                vehicle
            );
        }
        
        private void LogRaceOver(RaceResult result)
        {
            string winnerName = result.Winner != null ? result.Winner.vehicleName : "nobody";
            RaceHistory.Log(
                EventType.FinishLine,
                EventImportance.Critical,
                $"<color=#FFD700><b>RACE COMPLETE — {winnerName} wins! ({result.TotalParticipants} starters, {result.TotalRounds} rounds)</b></color>"
            );

            foreach (var record in result.Finishers)
            {
                RaceHistory.Log(
                    EventType.FinishLine,
                    EventImportance.High,
                    $"  #{record.Position} {record.Vehicle.vehicleName} — finished Round {record.Round}",
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
                    $"  DNF {record.Vehicle.vehicleName} — eliminated Round {record.Round} at {stageName} (progress {record.ProgressAtElimination})",
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
                $"{vehicle.vehicleName} rolled initiative: {initiative}",
                null,
                vehicle
            );
        }
        
        private void LogVehicleRemoved(Vehicle vehicle)
        {
            RaceHistory.Log(
                EventType.System,
                EventImportance.High,
                $"{vehicle.vehicleName} removed from turn order",
                RacePositionTracker.GetStage(vehicle),
                vehicle
            );
        }
        
        private void LogVehicleDestroyed(Vehicle vehicle)
        {
            RaceHistory.Log(
                EventType.Destruction,
                EventImportance.Critical,
                $"<color=#FF6600>{vehicle.vehicleName} has been destroyed!</color>",
                RacePositionTracker.GetStage(vehicle),
                vehicle
            );
        }

        private void LogVehicleFinished(Vehicle vehicle)
        {
            RaceHistory.Log(
                EventType.FinishLine,
                EventImportance.Critical,
                $"<color=#FFD700>{vehicle.vehicleName} has finished the race!</color>",
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
                $"{vehicle.vehicleName} automatically moved (movement not triggered manually)",
                RacePositionTracker.GetStage(vehicle),
                vehicle
            );
        }
        
        private void LogComponentPowerShutdown(Vehicle vehicle, VehicleComponent component, int required, int available)
        {
            RaceHistory.Log(
                EventType.Resource,
                EventImportance.Medium,
                $"{vehicle.vehicleName}: {component.name} shut down (needs {required}, have {available})",
                RacePositionTracker.GetStage(vehicle),
                vehicle
            );
        }
        
        private void LogMovementBlocked(Vehicle vehicle, string reason)
        {
            RaceHistory.Log(
                EventType.Movement,
                EventImportance.Medium,
                $"{vehicle.vehicleName} cannot move: {reason}",
                RacePositionTracker.GetStage(vehicle),
                vehicle
            );
        }
        
        private void LogMovementExecuted(Vehicle vehicle, int distance, int speed)
        {
            RaceHistory.Log(
                EventType.Movement,
                EventImportance.Low,
                $"{vehicle.vehicleName} moved {distance} units (speed {speed})",
                RacePositionTracker.GetStage(vehicle),
                vehicle
            );
        }
        
        private void LogStageEntered(Vehicle vehicle, Stage newStage, bool isPlayerChoice)
        {
            EventImportance importance = isPlayerChoice ? EventImportance.Medium : EventImportance.Low;

            RaceHistory.Log(
                EventType.Movement,
                importance,
                $"{vehicle.vehicleName} entered {newStage.stageName}",
                newStage,
                vehicle
            );
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
                RacePositionTracker.GetStage(vehicle),
                vehicle
            );
        }
        
        private void LogPlayerActionPhaseStarted(Vehicle vehicle)
        {
            RaceHistory.Log(
                EventType.System,
                EventImportance.Medium,
                $"{vehicle.vehicleName} can now take actions",
                RacePositionTracker.GetStage(vehicle),
                vehicle
            );
        }
        
        private void LogPlayerEndedTurn(Vehicle vehicle)
        {
            RaceHistory.Log(
                EventType.System,
                EventImportance.Low,
                $"{vehicle.vehicleName} ended turn",
                RacePositionTracker.GetStage(vehicle),
                vehicle
            );
        }
        
        private void LogPlayerTriggeredMovement(Vehicle vehicle)
        {
            RaceHistory.Log(
                EventType.Movement,
                EventImportance.Low,
                $"{vehicle.vehicleName} moved forward (player triggered)",
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
                $"Race initialized with {vehicleCount} vehicles and {stageCount} stages"
            );
        }
        
        public void LogTurnOrderEstablished(IReadOnlyList<Vehicle> vehicles)
        {
            string turnOrder = string.Join(", ", vehicles.Select(v => v.vehicleName));
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
                $"{vehicle.vehicleName} placed at starting position",
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
                    $"{vehicle.vehicleName} crew: {string.Join(", ", crewList)}",
                    stage,
                    vehicle
                );
            }
        }
    }
}

