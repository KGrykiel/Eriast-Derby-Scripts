using Assets.Scripts.Entities;
using Assets.Scripts.Entities.Vehicles;
using Assets.Scripts.Entities.Vehicles.VehicleComponents;
using UnityEngine;

namespace Assets.Scripts.Effects
{
    /// <summary>
    /// Used as a representation of any effect that can be applied to an entity like damage, status effects, resource restoration etc.
    /// Ideal for no-code usage in the editor.
    /// </summary>
    [System.Serializable]
    public abstract class EffectBase : IEffect
    {
        public abstract void Apply(IEffectTarget target, EffectContext context);

        /// <summary>
        /// Resolves an <see cref="IEffectTarget"/> to the <see cref="Entity"/> that should receive this effect.
        /// Default behaviour: Entity passes through, Vehicle routes to chassis.
        /// Override in subclasses that need custom vehicle-to-component routing.
        /// </summary>
        protected virtual Entity ResolveEntity(IEffectTarget target)
        {
            switch (target)
            {
                case Entity e:
                    return e;
                case Vehicle vehicle:
                    return vehicle.Chassis;
                case VehicleSeat:
                    Debug.LogWarning($"[{GetType().Name}] VehicleSeat is not a valid target for this effect.");
                    return null;
                default:
                    Debug.LogWarning($"[{GetType().Name}] Unsupported target type: {(target != null ? target.GetType().Name : "null")}");
                    return null;
            }
        }

        /// <summary>
        /// Resolves an <see cref="IEffectTarget"/> to the <see cref="VehicleSeat"/> that should receive this effect.
        /// Seats pass through; components navigate to the seat controlling them; Vehicles are rejected.
        /// </summary>
        protected virtual VehicleSeat ResolveSeat(IEffectTarget target)
        {
            switch (target)
            {
                case VehicleSeat seat:
                    return seat;
                case VehicleComponent component:
                    return ResolveSeatFromComponent(component);
                case Vehicle:
                    Debug.LogWarning($"[{GetType().Name}] Cannot apply to a Vehicle — which crew member? Use a component or VehicleSeat target.");
                    return null;
                default:
                    string typeName = target != null ? target.GetType().Name : "null";
                    Debug.LogWarning($"[{GetType().Name}] Unsupported target type: {typeName}. Use a component or VehicleSeat target.");
                    return null;
            }
        }

        private VehicleSeat ResolveSeatFromComponent(VehicleComponent component)
        {
            Vehicle vehicle = component.ParentVehicle;
            if (vehicle == null)
            {
                Debug.LogWarning($"[{GetType().Name}] Component '{component.name}' has no parent vehicle.");
                return null;
            }

            return vehicle.GetSeatForComponent(component);
        }

        /// <summary>
        /// Resolves an <see cref="IEffectTarget"/> to a <see cref="Vehicle"/>.
        /// Vehicles pass through; Entities navigate to their parent vehicle; Seats are rejected.
        /// </summary>
        protected virtual Vehicle ResolveVehicle(IEffectTarget target)
        {
            switch (target)
            {
                case Vehicle v:
                    return v;
                case Entity entity:
                    return EntityHelpers.GetParentVehicle(entity);
                case VehicleSeat:
                    Debug.LogWarning($"[{GetType().Name}] VehicleSeat is not a valid target for vehicle-level effects.");
                    return null;
                default:
                    Debug.LogWarning($"[{GetType().Name}] Unsupported target type: {(target != null ? target.GetType().Name : "null")}");
                    return null;
            }
        }
    }
}
