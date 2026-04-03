using System.Collections.Generic;
using Assets.Scripts.Entities.Vehicles;
using Assets.Scripts.Stages;

namespace Assets.Scripts.Managers.Logging.Results
{
    /// <summary>
    /// Tracks vehicle finish order and detects when all vehicles have either finished or been eliminated.
    /// Owns the sole responsibility of calling MarkAsFinished on vehicles that cross the finish line.
    /// </summary>
    public class RaceCompletionTracker
    {
        private readonly TurnStateMachine stateMachine;
        private readonly HashSet<Vehicle> allVehicles;
        private readonly List<RaceFinishRecord> finishers = new();
        private readonly List<RaceEliminationRecord> eliminationRecords = new();
        private readonly int maxRounds;
        private int nextPosition = 1;
        private bool raceOver = false;

        public IReadOnlyList<RaceFinishRecord> Finishers => finishers;

        public RaceCompletionTracker(TurnStateMachine stateMachine, IEnumerable<Vehicle> vehicles, int maxRounds)
        {
            this.stateMachine = stateMachine;
            this.maxRounds = maxRounds;
            allVehicles = new HashSet<Vehicle>(vehicles);
        }

        public void Subscribe()
        {
            TurnEventBus.OnFinishLineCrossed += HandleFinishLineCrossed;
            TurnEventBus.OnVehicleDestroyed += HandleVehicleDestroyed;
            TurnEventBus.OnRoundEnded += HandleRoundEnded;
        }

        public void Unsubscribe()
        {
            TurnEventBus.OnFinishLineCrossed -= HandleFinishLineCrossed;
            TurnEventBus.OnVehicleDestroyed -= HandleVehicleDestroyed;
            TurnEventBus.OnRoundEnded -= HandleRoundEnded;
        }

        private void HandleFinishLineCrossed(Vehicle vehicle, Stage finishStage)
        {
            if (raceOver) return;
            if (vehicle == null) return;
            if (vehicle.Status == VehicleStatus.Finished) return;

            vehicle.MarkAsFinished();

            finishers.Add(new RaceFinishRecord(vehicle, nextPosition, stateMachine.CurrentRound));
            nextPosition++;

            CheckRaceOver();
        }

        private void HandleVehicleDestroyed(Vehicle vehicle)
        {
            if (raceOver) return;
            if (vehicle == null) return;

            var record = new RaceEliminationRecord(
                vehicle,
                vehicle.CurrentStage,
                stateMachine.CurrentRound,
                vehicle.Progress);

            eliminationRecords.Add(record);
            CheckRaceOver();
        }

        private void HandleRoundEnded(int roundNumber)
        {
            if (raceOver) return;
            if (roundNumber < maxRounds) return;

            // Round cap reached — any vehicle not yet finished or eliminated is classified as DNF
            var accountedFor = new HashSet<Vehicle>();
            foreach (var record in finishers)
                accountedFor.Add(record.Vehicle);
            foreach (var record in eliminationRecords)
                accountedFor.Add(record.Vehicle);

            foreach (var vehicle in allVehicles)
            {
                if (accountedFor.Contains(vehicle)) continue;

                eliminationRecords.Add(new RaceEliminationRecord(
                    vehicle,
                    vehicle.CurrentStage,
                    roundNumber,
                    vehicle.Progress));
            }

            raceOver = true;
            var result = new RaceResult(finishers, eliminationRecords, allVehicles.Count, roundNumber);
            TurnEventBus.EmitRaceOver(result);
        }

        private void CheckRaceOver()
        {
            bool allAccountedFor = finishers.Count + eliminationRecords.Count >= allVehicles.Count;
            if (!allAccountedFor) return;

            raceOver = true;

            var result = new RaceResult(finishers, eliminationRecords, allVehicles.Count, stateMachine.CurrentRound);
            TurnEventBus.EmitRaceOver(result);
        }
    }
}
