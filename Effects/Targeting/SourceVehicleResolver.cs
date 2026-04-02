using System;
using System.Collections.Generic;
using Assets.Scripts.Combat.Rolls.RollSpecs;
using Assets.Scripts.Entities.Vehicles;
using SerializeReferenceEditor;
using UnityEngine;

namespace Assets.Scripts.Effects.Targeting
{
    /// <summary>
    /// Resolves to the acting (source) vehicle.
    /// Falls back to <c>ctx.Target as Vehicle</c> when there is no SourceActor in context
    /// (e.g. event card execution).
    /// </summary>
    [Serializable]
    [SRName("Source Vehicle")]
    public class SourceVehicleResolver : IEffectTargetResolver
    {
        public IReadOnlyList<IEffectTarget> Resolve(RollContext ctx)
        {
            Vehicle vehicle = null;
            if (ctx.SourceActor != null)
                vehicle = ctx.SourceActor.GetVehicle();
            if (vehicle == null)
                vehicle = ctx.Target as Vehicle;

            if (vehicle != null)
                return new IEffectTarget[] { vehicle };
            Debug.LogWarning("[SourceVehicleResolver] No source vehicle in context.");
            return Array.Empty<IEffectTarget>();
        }
    }
}
