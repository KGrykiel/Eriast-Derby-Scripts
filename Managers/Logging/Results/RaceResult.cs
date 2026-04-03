using System.Collections.Generic;
using Assets.Scripts.Entities.Vehicles;

namespace Assets.Scripts.Managers.Logging.Results
{
    /// <summary>Complete record of a finished race — finishing order, eliminations, and overall stats.</summary>
    public class RaceResult
    {
        /// <summary>Vehicles that crossed the finish line, in finishing order.</summary>
        public IReadOnlyList<RaceFinishRecord> Finishers { get; }

        /// <summary>Vehicles that were destroyed before finishing, with elimination details.</summary>
        public IReadOnlyList<RaceEliminationRecord> DidNotFinish { get; }

        /// <summary>Total number of vehicles that started the race.</summary>
        public int TotalParticipants { get; }

        /// <summary>Round number at the moment the race ended.</summary>
        public int TotalRounds { get; }

        public Vehicle Winner => Finishers.Count > 0 ? Finishers[0].Vehicle : null;
        public bool HasFinishers => Finishers.Count > 0;

        public RaceResult(
            IReadOnlyList<RaceFinishRecord> finishers,
            IReadOnlyList<RaceEliminationRecord> didNotFinish,
            int totalParticipants,
            int totalRounds)
        {
            Finishers = finishers;
            DidNotFinish = didNotFinish;
            TotalParticipants = totalParticipants;
            TotalRounds = totalRounds;
        }
    }
}
