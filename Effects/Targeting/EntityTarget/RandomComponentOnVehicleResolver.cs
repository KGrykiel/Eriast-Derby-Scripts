using System;
using System.Collections.Generic;
using Assets.Scripts.Combat.Rolls.RollSpecs;
using Assets.Scripts.Entities;
using Assets.Scripts.Entities.Vehicles;
using SerializeReferenceEditor;
using UnityEngine;

namespace Assets.Scripts.Effects.Targeting.EntityTarget
{
    /// <summary>
    /// Resolves to one randomly selected component on the chosen vehicle.
    /// Use <see cref="VehicleSource"/> to select the source or target vehicle.
    /// </summary>
    [Serializable]
    [SRName("Random On Vehicle")]
    public class RandomComponentOnVehicleResolver : IEntityEffectResolver
    {
        public VehicleSource source = VehicleSource.TargetVehicle;

        public IReadOnlyList<Entity> Resolve(RollContext ctx)
        {
            Vehicle vehicle = ResolveVehicle(ctx);
            if (vehicle == null)
            {
                Debug.LogWarning($"[RandomComponentOnVehicleResolver] No {source} in context.");
                return Array.Empty<Entity>();
            }

            var components = vehicle.AllComponents;
            if (components.Count > 0)
                return new Entity[] { components[UnityEngine.Random.Range(0, components.Count)] };
            return Array.Empty<Entity>();
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
