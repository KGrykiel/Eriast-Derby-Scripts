using System;
using System.Collections.Generic;
using Assets.Scripts.Combat.Rolls.RollSpecs;
using Assets.Scripts.Entities.Vehicles;
using SerializeReferenceEditor;
using UnityEngine;

namespace Assets.Scripts.Effects.Targeting.VehicleTarget
{
    /// <summary>
    /// Resolves to the acting (source) vehicle.
    /// </summary>
    [Serializable]
    [SRName("Source Vehicle")]
    public class SourceVehicleResolver : IVehicleEffectResolver
    {
        public IReadOnlyList<Vehicle> Resolve(RollContext ctx)
        {
            Vehicle vehicle = ctx.SourceActor?.GetVehicle();

            if (vehicle != null)
                return new Vehicle[] { vehicle };

            Debug.LogWarning($"[SourceVehicleResolver] No source vehicle in context. Causal source: {ctx.CausalSource ?? "unknown"}");
            return Array.Empty<Vehicle>();
        }
    }
}
