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
    /// Resolves to the target vehicle, falling back to the source vehicle if no target vehicle
    /// can be derived from the context.
    /// </summary>
    [Serializable]
    [SRName("Target Vehicle")]
    public class TargetVehicleResolver : IEffectTargetResolver
    {
        public IReadOnlyList<IEffectTarget> Resolve(RollContext ctx)
        {
            Vehicle vehicle = EntityHelpers.GetVehicleFromTarget(ctx.Target);
            if (vehicle == null)
                vehicle = GetSourceVehicle(ctx);

            if (vehicle != null)
                return new IEffectTarget[] { vehicle };
            Debug.LogWarning("[TargetVehicleResolver] No target or source vehicle in context.");
            return Array.Empty<IEffectTarget>();
        }

        private static Vehicle GetSourceVehicle(RollContext ctx)
        {
            if (ctx.SourceActor != null)
            {
                Vehicle vehicle = ctx.SourceActor.GetVehicle();
                if (vehicle != null) return vehicle;
            }
            return ctx.Target as Vehicle;
        }
    }
}
