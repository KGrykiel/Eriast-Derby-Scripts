using System.Linq;
using Assets.Scripts.Entities.Vehicle.VehicleComponents.ComponentTypes;

namespace Assets.Scripts.Entities.Vehicle
{
    /// <summary>
    /// Handles effect routing and modifier targeting for a vehicle.
    /// Determines which component should receive effects and modifiers based on effect type and attributes.
    /// 
    /// Extracted from Vehicle.cs to separate concerns.
    /// Vehicle delegates to this coordinator for all effect routing logic.
    /// </summary>
    public class VehicleEffectRouter
    {
        private readonly global::Vehicle vehicle;

        public VehicleEffectRouter(global::Vehicle vehicle)
        {
            this.vehicle = vehicle;
        }

        // ==================== EFFECT ROUTING ====================

        /// <summary>
        /// Route an effect to the appropriate component.
        /// If playerSelectedComponent provided, uses it. Otherwise auto-routes based on effect type.
        /// </summary>
        public Entity RouteEffectTarget(IEffect effect, VehicleComponent playerSelectedComponent = null)
        {
            // Use player selection if provided
            if (playerSelectedComponent != null)
                return playerSelectedComponent;

            // Otherwise auto-route based on effect type and attributes
            return RouteEffectByAttribute(effect);
        }

        /// <summary>
        /// Route effect to appropriate component by analyzing its attributes.
        /// Used for auto-routing (non-precise targeting).
        /// </summary>
        private Entity RouteEffectByAttribute(IEffect effect)
        {
            if (effect == null)
                return vehicle.chassis;

            // Direct damage always goes to chassis
            if (effect is DamageEffect)
                return vehicle.chassis;

            // Healing/restoration goes to chassis
            // TODO: Consider energy restoration routing to power core?
            if (effect is ResourceRestorationEffect)
                return vehicle.chassis;

            // Attribute modifiers route by attribute
            if (effect is AttributeModifierEffect modifierEffect)
            {
                VehicleComponent component = ResolveModifierTarget(modifierEffect.attribute);
                return component != null ? component : vehicle.chassis;
            }

            // Status effects route by their first modifier's attribute
            if (effect is ApplyStatusEffect statusEffect)
            {
                if (statusEffect.statusEffect != null && 
                    statusEffect.statusEffect.modifiers != null && 
                    statusEffect.statusEffect.modifiers.Count > 0)
                {
                    var firstModifier = statusEffect.statusEffect.modifiers[0];
                    VehicleComponent component = ResolveModifierTarget(firstModifier.attribute);
                    return component != null ? component : vehicle.chassis;
                }
                // No modifiers - default to chassis for behavioral effects (stun, etc.)
                return vehicle.chassis;
            }

            // Unknown effect type - default to chassis
            return vehicle.chassis;
        }

        // ==================== MODIFIER TARGET RESOLUTION ====================

        /// <summary>
        /// Resolve which component should receive a modifier based on the attribute being modified.
        /// Used by cross-component modifiers and effect routing.
        /// </summary>
        public VehicleComponent ResolveModifierTarget(Attribute attribute)
        {
            return attribute switch
            {
                Attribute.MaxHealth => vehicle.chassis,
                Attribute.ArmorClass => vehicle.chassis,
                Attribute.MagicResistance => vehicle.chassis,
                Attribute.Mobility => vehicle.chassis,
                Attribute.DragCoefficient => vehicle.chassis,
                Attribute.MaxEnergy => vehicle.powerCore,
                Attribute.EnergyRegen => vehicle.powerCore,
                Attribute.MaxSpeed => vehicle.optionalComponents.OfType<DriveComponent>().FirstOrDefault(),
                Attribute.Acceleration => vehicle.optionalComponents.OfType<DriveComponent>().FirstOrDefault(),
                Attribute.Stability => vehicle.optionalComponents.OfType<DriveComponent>().FirstOrDefault(),
                Attribute.BaseFriction => vehicle.optionalComponents.OfType<DriveComponent>().FirstOrDefault(),
                _ => vehicle.chassis
            };
        }
    }
}
