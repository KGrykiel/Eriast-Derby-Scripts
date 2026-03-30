using System.Collections.Generic;
using Assets.Scripts.Combat.Rolls.RollSpecs;
using Assets.Scripts.Entities;
using Assets.Scripts.Entities.Vehicles;

namespace Assets.Scripts.Combat.Rolls.Targeting
{
    /// <summary>
    /// Resolves to all vehicles currently in the same stage as the context vehicle.
    /// Replaces <c>CardTargetMode.AllInStage</c>.
    /// </summary>
    public class AllVehiclesInStageResolver : ITargetResolver
    {
        /// <summary>When true, excludes the caster's vehicle (ctx.SourceActor.GetVehicle()) from results.</summary>
        public bool ExcludeSelf;

        /// <summary>When true, excludes the primary target vehicle (derived from ctx.Target) from results.</summary>
        public bool ExcludeTarget;

        public AllVehiclesInStageResolver(bool excludeSelf = false, bool excludeTarget = false)
        {
            ExcludeSelf = excludeSelf;
            ExcludeTarget = excludeTarget;
        }

        public IReadOnlyList<IRollTarget> ResolveFrom(RollContext ctx)
        {
            Vehicle vehicle = ctx.SourceActor?.GetVehicle();
            if (vehicle == null)
                vehicle = EntityHelpers.GetVehicleFromTarget(ctx.Target);
            if (vehicle == null || vehicle.currentStage == null)
                return System.Array.Empty<IRollTarget>();

            Vehicle self = ExcludeSelf ? vehicle : null;
            Vehicle primaryTarget = ExcludeTarget ? EntityHelpers.GetVehicleFromTarget(ctx.Target) : null;

            var results = new List<IRollTarget>();
            foreach (var v in vehicle.currentStage.vehiclesInStage)
            {
                if (v != null && v != self && v != primaryTarget)
                    results.Add(v);
            }
            return results;
        }
    }
}
