using System.Collections.Generic;
using Assets.Scripts.Combat.Rolls.RollSpecs;
using Assets.Scripts.Entities;
using Assets.Scripts.Entities.Vehicles.VehicleComponents;
using SerializeReferenceEditor;
using UnityEngine;

namespace Assets.Scripts.Effects.Targeting.EntityTarget
{
    /// <summary>
    /// Wraps an <see cref="IVehicleEffectResolver"/> and maps each resolved vehicle to its components
    /// of the specified type. Defaults to <see cref="ComponentType.Chassis"/>.
    /// Use a different type to target specific components across vehicles (e.g., all Drives in the lane).
    /// </summary>
    [System.Serializable]
    [SRName("Via Vehicles")]
    public class EntitiesViaVehiclesResolver : IEntityEffectResolver
    {
        [SerializeReference, SR]
        public IVehicleEffectResolver vehicleResolver;

        public ComponentType componentType = ComponentType.Chassis;

        public IReadOnlyList<Entity> Resolve(RollContext ctx)
        {
            if (vehicleResolver == null)
            {
                Debug.LogWarning("[EntitiesViaVehiclesResolver] No vehicle resolver set.");
                return System.Array.Empty<Entity>();
            }

            var vehicles = vehicleResolver.Resolve(ctx);
            var results = new List<Entity>(vehicles.Count);
            foreach (var vehicle in vehicles)
            {
                if (vehicle == null) continue;

                foreach (var component in vehicle.AllComponents)
                {
                    if (component != null && component.componentType == componentType)
                        results.Add(component);
                }
            }
            return results;
        }
    }
}
