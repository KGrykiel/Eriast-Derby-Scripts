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
    /// Resolves to the target vehicle.
    /// </summary>
    [Serializable]
    [SRName("Target Vehicle")]
    public class TargetVehicleResolver : IVehicleEffectResolver
    {
        public IReadOnlyList<Vehicle> Resolve(RollContext ctx)
        {
            Vehicle vehicle = EntityHelpers.GetVehicleFromTarget(ctx.Target);

            if (vehicle != null)
                return new Vehicle[] { vehicle };
            Debug.LogWarning("[TargetVehicleResolver] No target vehicle in context.");
            return Array.Empty<Vehicle>();
        }
    }
}
