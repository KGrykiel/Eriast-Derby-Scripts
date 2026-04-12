using System.Collections.Generic;
using Assets.Scripts.Stages;
using Assets.Scripts.Stages.Lanes;
using Assets.Scripts.Entities.Vehicles;

namespace Assets.Scripts.Managers
{
    /// <summary>
    /// Tracks race positions for all active vehicles — current stage, lane, and progress.
    /// Vehicles self-register on OnEnable and deregister on OnDisable.
    /// </summary>
    public static class RacePositionTracker
    {
        private class RacePosition
        {
            public Stage Stage;
            public StageLane Lane;
            public int Progress;
        }

        private static readonly Dictionary<Vehicle, RacePosition> _positions = new();

        public static void Register(Vehicle vehicle)
        {
            if (vehicle == null) return;
            if (!_positions.ContainsKey(vehicle))
                _positions[vehicle] = new RacePosition();
        }

        public static void Unregister(Vehicle vehicle) => _positions.Remove(vehicle);

        public static Stage GetStage(Vehicle vehicle)
        {
            if (vehicle == null || !_positions.TryGetValue(vehicle, out var pos)) return null;
            return pos.Stage;
        }

        public static StageLane GetLane(Vehicle vehicle)
        {
            if (vehicle == null || !_positions.TryGetValue(vehicle, out var pos)) return null;
            return pos.Lane;
        }

        public static int GetProgress(Vehicle vehicle)
        {
            if (vehicle == null || !_positions.TryGetValue(vehicle, out var pos)) return 0;
            return pos.Progress;
        }

        public static void SetStage(Vehicle vehicle, Stage stage)
        {
            if (vehicle == null) return;
            EnsureRegistered(vehicle).Stage = stage;
        }

        public static void SetLane(Vehicle vehicle, StageLane lane)
        {
            if (vehicle == null) return;
            EnsureRegistered(vehicle).Lane = lane;
        }

        public static void SetProgress(Vehicle vehicle, int progress)
        {
            if (vehicle == null) return;
            EnsureRegistered(vehicle).Progress = progress;
        }

        /// <summary>Returns all tracked vehicles currently positioned in the given stage.</summary>
        public static IReadOnlyList<Vehicle> GetVehiclesInStage(Stage stage)
        {
            if (stage == null) return System.Array.Empty<Vehicle>();
            var result = new List<Vehicle>();
            foreach (var kvp in _positions)
            {
                if (kvp.Value.Stage == stage)
                    result.Add(kvp.Key);
            }
            return result;
        }

        /// <summary>Returns a snapshot of all currently registered vehicles.</summary>
        public static List<Vehicle> GetAll() => new(_positions.Keys);

        /// <summary>Returns all tracked vehicles currently assigned to the given lane.</summary>
        public static IReadOnlyList<Vehicle> GetVehiclesInLane(StageLane lane)
        {
            if (lane == null) return System.Array.Empty<Vehicle>();
            var result = new List<Vehicle>();
            foreach (var kvp in _positions)
            {
                if (kvp.Value.Lane == lane)
                    result.Add(kvp.Key);
            }
            return result;
        }

        private static RacePosition EnsureRegistered(Vehicle vehicle)
        {
            if (!_positions.TryGetValue(vehicle, out var pos))
            {
                pos = new RacePosition();
                _positions[vehicle] = pos;
            }
            return pos;
        }
    }
}
