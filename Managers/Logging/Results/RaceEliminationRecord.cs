using Assets.Scripts.Entities.Vehicles;
using Assets.Scripts.Stages;

namespace Assets.Scripts.Managers.Logging.Results
{
    /// <summary>Records where and when a vehicle was eliminated before finishing the race.</summary>
    public readonly struct RaceEliminationRecord
    {
        public Vehicle Vehicle { get; }

        /// <summary>Stage the vehicle was in when it was destroyed.</summary>
        public Stage EliminatedAt { get; }

        /// <summary>Round in which the vehicle was destroyed.</summary>
        public int Round { get; }

        /// <summary>Progress within the elimination stage at the time of destruction.</summary>
        public int ProgressAtElimination { get; }

        public RaceEliminationRecord(Vehicle vehicle, Stage eliminatedAt, int round, int progressAtElimination)
        {
            Vehicle = vehicle;
            EliminatedAt = eliminatedAt;
            Round = round;
            ProgressAtElimination = progressAtElimination;
        }
    }
}
