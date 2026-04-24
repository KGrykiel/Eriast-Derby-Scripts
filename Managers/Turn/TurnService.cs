using System.Collections.Generic;
using Assets.Scripts.Stages;
using Assets.Scripts.Entities.Vehicles;
using Assets.Scripts.Managers.Race;

namespace Assets.Scripts.Managers.Turn
{
    /// <summary>Owns the vehicle roster and provides targeting queries scoped to stages and teams.</summary>
    public class TurnService
    {
        private readonly List<Vehicle> vehicles;

        public IReadOnlyList<Vehicle> AllVehicles => vehicles;

        public TurnService(List<Vehicle> vehicleList)
        {
            vehicles = vehicleList ?? new List<Vehicle>();
        }

        // ==================== COMBAT ====================

        /// <summary>Returns all active vehicles in the same stage as <paramref name="source"/>, excluding source itself.</summary>
        public List<Vehicle> GetOtherVehiclesInStage(Vehicle source)
        {
            if (source == null) return new List<Vehicle>();
            Stage sourceStage = RacePositionTracker.GetStage(source);
            if (sourceStage == null)
                return new List<Vehicle>();

            var others = new List<Vehicle>();
            foreach (var v in vehicles)
            {
                if (v == source) continue;
                if (RacePositionTracker.GetStage(v) != sourceStage) continue;
                if (v.Status != VehicleStatus.Active) continue;
                others.Add(v);
            }

            return others;
        }

        /// <summary>Returns all active vehicles in the same stage as <paramref name="source"/>, including source itself.</summary>
        public List<Vehicle> GetAllTargets(Vehicle source)
        {
            if (source == null) return new List<Vehicle>();
            Stage sourceStage = RacePositionTracker.GetStage(source);
            if (sourceStage == null)
                return new List<Vehicle>();

            var targets = new List<Vehicle>();
            foreach (var v in vehicles)
            {
                if (RacePositionTracker.GetStage(v) != sourceStage) continue;
                if (v.Status != VehicleStatus.Active) continue;
                targets.Add(v);
            }

            return targets;
        }

        /// <summary>Returns all active allies of <paramref name="source"/> in the same stage.</summary>
        public List<Vehicle> GetAlliedTargets(Vehicle source)
        {
            if (source == null || source.team == null) return new List<Vehicle>();
            Stage sourceStage = RacePositionTracker.GetStage(source);
            if (sourceStage == null)
                return new List<Vehicle>();

            var allies = new List<Vehicle>();
            foreach (var v in vehicles)
            {
                if (v == source) continue;
                if (v.team != source.team) continue;
                if (RacePositionTracker.GetStage(v) != sourceStage) continue;
                if (v.Status != VehicleStatus.Active) continue;
                allies.Add(v);
            }

            return allies;
        }

        /// <summary>
        /// True if both vehicles share the same non-null team.
        /// Independent vehicles (team == null) are never allied with anyone.
        /// </summary>
        public static bool AreAllied(Vehicle a, Vehicle b)
        {
            if (a == null || b == null) return false;
            if (a.team == null || b.team == null) return false;
            return a.team == b.team;
        }

        /// <summary>
        /// True if the two vehicles are not on the same team.
        /// Independent vehicles (team == null) are hostile to everyone, including other independents.
        /// </summary>
        public static bool AreHostile(Vehicle a, Vehicle b)
        {
            if (a == null || b == null) return false;
            if (a == b) return false;
            return !AreAllied(a, b);
        }
    }
}