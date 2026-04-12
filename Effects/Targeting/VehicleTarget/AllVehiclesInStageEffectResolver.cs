using System;
using System.Collections.Generic;
using Assets.Scripts.Combat.Rolls.RollSpecs;
using Assets.Scripts.Entities;
using Assets.Scripts.Entities.Vehicles;
using Assets.Scripts.Managers;
using SerializeReferenceEditor;
using UnityEngine;

namespace Assets.Scripts.Effects.Targeting.VehicleTarget
{
    /// <summary>
    /// Resolves to all vehicles in the same stage as the source vehicle.
    /// Stage is derived from the source vehicle; falls back to the target vehicle when no SourceActor is present.
    /// </summary>
    [Serializable]
    [SRName("All In Stage")]
    public class AllVehiclesInStageEffectResolver : IVehicleEffectResolver
    {
        /// <summary>When true, excludes the source vehicle from results.</summary>
        public bool ExcludeSelf;

        /// <summary>When true, excludes the primary target vehicle (derived from ctx.Target) from results.</summary>
        public bool ExcludeTarget;

        public IReadOnlyList<Vehicle> Resolve(RollContext ctx)
        {
            Vehicle sourceVehicle = null;
            if (ctx.SourceActor != null)
                sourceVehicle = ctx.SourceActor.GetVehicle();
            if (sourceVehicle == null)
                sourceVehicle = ctx.Target as Vehicle;

            var stage = RacePositionTracker.GetStage(sourceVehicle);
            if (sourceVehicle == null || stage == null)
            {
                Debug.LogWarning("[AllVehiclesInStageEffectResolver] No source vehicle or stage in context.");
                return Array.Empty<Vehicle>();
            }

            Vehicle self = ExcludeSelf ? sourceVehicle : null;
            Vehicle primaryTarget = ExcludeTarget ? EntityHelpers.GetVehicleFromTarget(ctx.Target) : null;

            var results = new List<Vehicle>();
            foreach (var v in RacePositionTracker.GetVehiclesInStage(stage))
            {
                if (v != null && v != self && v != primaryTarget)
                    results.Add(v);
            }
            return results;
        }
    }
}
