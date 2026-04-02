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
    /// Resolves to one randomly selected component on the chosen vehicle.
    /// Use <see cref="VehicleSource"/> to select the source or target vehicle.
    /// </summary>
    [Serializable]
    [SRName("Entity/Random On Vehicle")]
    public class RandomComponentOnVehicleResolver : IEffectTargetResolver
    {
        public VehicleSource source = VehicleSource.TargetVehicle;

        public IReadOnlyList<IEffectTarget> Resolve(RollContext ctx)
        {
            Vehicle vehicle = ResolveVehicle(ctx);
            if (vehicle == null)
            {
                Debug.LogWarning($"[RandomComponentOnVehicleResolver] No {source} in context.");
                return Array.Empty<IEffectTarget>();
            }

            var components = vehicle.AllComponents;
            if (components.Count > 0)
                return new IEffectTarget[] { components[UnityEngine.Random.Range(0, components.Count)] };
            return Array.Empty<IEffectTarget>();
        }

        private Vehicle ResolveVehicle(RollContext ctx)
        {
            if (source == VehicleSource.SourceVehicle)
            {
                if (ctx.SourceActor != null)
                {
                    Vehicle vehicle = ctx.SourceActor.GetVehicle();
                    if (vehicle != null) return vehicle;
                }
                return ctx.Target as Vehicle;
            }
            return EntityHelpers.GetVehicleFromTarget(ctx.Target);
        }
    }
}
