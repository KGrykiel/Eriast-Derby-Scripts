using System.Collections.Generic;
using Assets.Scripts.Combat.Rolls.RollSpecs;
using Assets.Scripts.Entities;
using Assets.Scripts.Entities.Vehicles;
using Assets.Scripts.Stages.Lanes;

namespace Assets.Scripts.Combat.Rolls.Targeting
{
    /// <summary>
    /// Resolves to all vehicles in the context target's lane.
    /// If <c>ctx.Target</c> is a <see cref="StageLane"/>, uses it directly.
    /// Otherwise derives the lane from the target vehicle.
    /// </summary>
    public class AllVehiclesInLaneResolver : ITargetResolver
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
                return System.Array.Empty<IRollTarget>();

            Vehicle self = ExcludeSelf && ctx.SourceActor != null ? ctx.SourceActor.GetVehicle() : null;
            Vehicle primaryTarget = ExcludeTarget ? ctx.Target switch
            {
                Vehicle v        => v,
                Entity entity    => EntityHelpers.GetParentVehicle(entity),
                VehicleSeat seat => seat.ParentVehicle,
                _                => null
            } : null;

            var results = new List<IRollTarget>();
            foreach (var v in lane.vehiclesInLane)
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

            Vehicle vehicle = ctx.Target switch
            {
                Vehicle v => v,
                Entity entity => EntityHelpers.GetParentVehicle(entity),
                VehicleSeat seat => seat.ParentVehicle,
                _ => null
            };
            return vehicle != null ? vehicle.currentLane : null;
        }
    }
}
