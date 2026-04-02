using System;
using System.Collections.Generic;
using Assets.Scripts.Combat.Rolls.RollSpecs;
using Assets.Scripts.Entities;
using Assets.Scripts.Entities.Vehicles;
using Assets.Scripts.Stages.Lanes;
using SerializeReferenceEditor;
using UnityEngine;

namespace Assets.Scripts.Effects.Targeting
{
    /// <summary>
    /// Resolves to all vehicles in the lane derived from the roll context target.
    /// If <c>ctx.Target</c> is a <see cref="StageLane"/>, uses it directly;
    /// otherwise derives the lane from the target vehicle.
    /// When <see cref="SourceActor"/> is null and <see cref="ExcludeSelf"/> is true,
    /// falls back to excluding <c>ctx.Target as Vehicle</c> (event card context).
    /// </summary>
    [Serializable]
    [SRName("Vehicle/All In Lane")]
    public class AllVehiclesInLaneEffectResolver : IEffectTargetResolver
    {
        /// <summary>When true, excludes the caster's vehicle from results. Falls back to excluding the target vehicle when no SourceActor is present.</summary>
        public bool ExcludeSelf;

        /// <summary>When true, excludes the primary target vehicle (derived from ctx.Target) from results.</summary>
        public bool ExcludeTarget;

        public IReadOnlyList<IEffectTarget> Resolve(RollContext ctx)
        {
            StageLane lane = ResolveLane(ctx);
            if (lane == null)
            {
                Debug.LogWarning("[AllVehiclesInLaneEffectResolver] No lane in context.");
                return Array.Empty<IEffectTarget>();
            }

            Vehicle selfVehicle = null;
            if (ExcludeSelf)
            {
                if (ctx.SourceActor != null)
                    selfVehicle = ctx.SourceActor.GetVehicle();
                if (selfVehicle == null)
                    selfVehicle = ctx.Target as Vehicle;
            }

            Vehicle primaryTarget = ExcludeTarget ? EntityHelpers.GetVehicleFromTarget(ctx.Target) : null;

            var results = new List<IEffectTarget>();
            foreach (var v in lane.vehiclesInLane)
            {
                if (v != null && v != selfVehicle && v != primaryTarget)
                    results.Add(v);
            }
            return results;
        }

        private static StageLane ResolveLane(RollContext ctx)
        {
            if (ctx.Target is StageLane lane)
                return lane;
            Vehicle vehicle = EntityHelpers.GetVehicleFromTarget(ctx.Target);
            return vehicle != null ? vehicle.CurrentLane : null;
        }
    }
}
