using Assets.Scripts.Entities.Vehicles;

namespace Assets.Scripts.Managers.Logging.Results
{
    /// <summary>Records a single vehicle's finishing position and the round it crossed the finish line.</summary>
    public readonly struct RaceFinishRecord
    {
        public Vehicle Vehicle { get; }
        public int Position { get; }
        public int Round { get; }

        public RaceFinishRecord(Vehicle vehicle, int position, int round)
        {
            Vehicle = vehicle;
            Position = position;
            Round = round;
        }
    }
}
