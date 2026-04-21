using System;
using System.Collections.Generic;
using Assets.Scripts.Combat.Rolls.RollSpecs;
using Assets.Scripts.Entities;
using Assets.Scripts.Entities.Vehicles;
using Assets.Scripts.Managers.Race;
using Assets.Scripts.Stages.Lanes;
using SerializeReferenceEditor;

namespace Assets.Scripts.Combat.Rolls.Targeting
{
    /// <summary>
    /// Resolves to all vehicles in the context target's lane.
    /// If <c>ctx.Target</c> is a <see cref="StageLane"/>, uses it directly.
    /// Otherwise derives the lane from the target vehicle.
    /// </summary>
    [Serializable]
    [SRName("All Vehicles In Lane")]
    public class AllVehiclesInLaneResolver : IRollTargetResolver
    {
        /// <summary>When true, excludes the caster's vehicle (ctx.SourceActor.GetVehicle()) from results.</summary>
        public bool ExcludeSelf;

        /// <summary>When true, excludes the primary target vehicle (derived from ctx.Target) from results.</summary>
        public bool ExcludeTarget;

        public AllVehiclesInLaneResolver(bool excludeSelf = false, bool excludeTarget = false)
        {
            ExcludeSelf = excludeSelf;
            ExcludeTarget = excludeTarget;
        }

        public IReadOnlyList<IRollTarget> ResolveFrom(RollContext ctx)
        {
            StageLane lane = ResolveLane(ctx);
            if (lane == null)
                return Array.Empty<IRollTarget>();

            Vehicle self = ExcludeSelf && ctx.SourceActor != null ? ctx.SourceActor.GetVehicle() : null;
            Vehicle primaryTarget = ExcludeTarget ? EntityHelpers.GetVehicleFromTarget(ctx.Target) : null;

            var results = new List<IRollTarget>();
            foreach (var v in RacePositionTracker.GetVehiclesInLane(lane))
            {
                if (v != null && v != self && v != primaryTarget)
                    results.Add(v);
            }
            return results;
        }

        private static StageLane ResolveLane(RollContext ctx)
        {
            if (ctx.Target is StageLane lane)
                return lane;

            Vehicle vehicle = EntityHelpers.GetVehicleFromTarget(ctx.Target);
            return vehicle != null ? RacePositionTracker.GetLane(vehicle) : null;
        }
    }
}
