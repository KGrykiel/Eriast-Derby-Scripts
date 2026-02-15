using System.Linq;

namespace Assets.Scripts.Entities.Vehicle
{
    /// <summary>Routes effects and modifiers to the correct component based on type/attribute.</summary>
    public class VehicleEffectRouter
    {
        private readonly global::Vehicle vehicle;

        public VehicleEffectRouter(global::Vehicle vehicle)
        {
            this.vehicle = vehicle;
        }

        // ==================== EFFECT ROUTING ====================

        public Entity RouteEffectTarget(IEffect effect)
        {
            // damaging the "vehicle" is always damaging the chassis. The chassis is the vehicle.
            if (effect is DamageEffect)
                return vehicle.chassis;

            // TODO: energy should be routed to power core, but we don't have any energy restoration effects yet, so for now just route to chassis
            if (effect is ResourceRestorationEffect)
                return vehicle.chassis;

            if (effect is AttributeModifierEffect modifierEffect)
            {
                VehicleComponent component = ResolveModifierTarget(modifierEffect.attribute);
                return component != null ? component : vehicle.chassis;
            }

            if (effect is ApplyStatusEffect statusEffect)
            {
                if (statusEffect.statusEffect != null && 
                    statusEffect.statusEffect.modifiers != null && 
                    statusEffect.statusEffect.modifiers.Count > 0)
                {
                    //TODO: this is a bit of a band-aid solution. For status effects with multiple modifiers,
                    //we should ideally be applying each modifier to the correct component instead of just routing based on the first modifier's attribute.
                    //For now, we'll just route based on the first modifier.
                    var firstModifier = statusEffect.statusEffect.modifiers[0];
                    VehicleComponent component = ResolveModifierTarget(firstModifier.attribute);
                    return component != null ? component : vehicle.chassis;
                }
                return vehicle.chassis;
            }

            return vehicle.chassis;
        }

        // ==================== MODIFIER TARGET RESOLUTION ====================

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
