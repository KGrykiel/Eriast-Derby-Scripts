using Assets.Scripts.Entities;
using Assets.Scripts.Entities.Vehicles;
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
                    return vehicle.chassis;
                case VehicleSeat:
                    Debug.LogWarning($"[{GetType().Name}] VehicleSeat is not a valid target for this effect.");
                    return null;
                default:
                    Debug.LogWarning($"[{GetType().Name}] Unsupported target type: {(target != null ? target.GetType().Name : "null")}");
                    return null;
            }
        }
    }
}
