using System;
using System.Collections.Generic;
using Assets.Scripts.Combat.Rolls.RollSpecs;
using Assets.Scripts.Entities;
using Assets.Scripts.Entities.Vehicles;
using Assets.Scripts.Managers;
using SerializeReferenceEditor;

namespace Assets.Scripts.Combat.Rolls.Targeting
{
    /// <summary>
    /// Resolves to all vehicles currently in the same stage as the context vehicle.
    /// Replaces <c>CardTargetMode.AllInStage</c>.
    /// </summary>
    [Serializable]
    [SRName("All Vehicles In Stage")]
    public class AllVehiclesInStageResolver : IRollTargetResolver
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
            var stage = RacePositionTracker.GetStage(vehicle);
            if (vehicle == null || stage == null)
                return Array.Empty<IRollTarget>();

            Vehicle self = ExcludeSelf ? vehicle : null;
            Vehicle primaryTarget = ExcludeTarget ? EntityHelpers.GetVehicleFromTarget(ctx.Target) : null;

            var results = new List<IRollTarget>();
            foreach (var v in RacePositionTracker.GetVehiclesInStage(stage))
            {
                if (v != null && v != self && v != primaryTarget)
                    results.Add(v);
            }
            return results;
        }
    }
}
