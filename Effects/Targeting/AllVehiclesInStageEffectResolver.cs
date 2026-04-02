using System;
using System.Collections.Generic;
using Assets.Scripts.Combat.Rolls.RollSpecs;
using Assets.Scripts.Entities;
using Assets.Scripts.Entities.Vehicles;
using SerializeReferenceEditor;
using UnityEngine;

namespace Assets.Scripts.Effects.Targeting
{
    /// <summary>
    /// Resolves to all vehicles in the same stage as the source vehicle.
    /// Stage is derived from the source vehicle; falls back to the target vehicle when no SourceActor is present.
    /// </summary>
    [Serializable]
    [SRName("Vehicle/All In Stage")]
    public class AllVehiclesInStageEffectResolver : IEffectTargetResolver
    {
        /// <summary>When true, excludes the source vehicle from results.</summary>
        public bool ExcludeSelf;

        /// <summary>When true, excludes the primary target vehicle (derived from ctx.Target) from results.</summary>
        public bool ExcludeTarget;

        public IReadOnlyList<IEffectTarget> Resolve(RollContext ctx)
        {
            Vehicle sourceVehicle = null;
            if (ctx.SourceActor != null)
                sourceVehicle = ctx.SourceActor.GetVehicle();
            if (sourceVehicle == null)
                sourceVehicle = ctx.Target as Vehicle;

            if (sourceVehicle == null || sourceVehicle.CurrentStage == null)
            {
                Debug.LogWarning("[AllVehiclesInStageEffectResolver] No source vehicle or stage in context.");
                return Array.Empty<IEffectTarget>();
            }

            Vehicle self = ExcludeSelf ? sourceVehicle : null;
            Vehicle primaryTarget = ExcludeTarget ? EntityHelpers.GetVehicleFromTarget(ctx.Target) : null;

            var results = new List<IEffectTarget>();
            foreach (var v in sourceVehicle.CurrentStage.vehiclesInStage)
            {
                if (v != null && v != self && v != primaryTarget)
                    results.Add(v);
            }
            return results;
        }
    }
}
