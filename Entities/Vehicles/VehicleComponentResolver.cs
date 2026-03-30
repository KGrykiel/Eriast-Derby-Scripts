using Assets.Scripts.Entities.Vehicles.VehicleComponents;
using Assets.Scripts.Entities.Vehicles.VehicleComponents.ComponentTypes;
using System.Linq;

namespace Assets.Scripts.Entities.Vehicles
{
    /// <summary>
    /// Single source of truth for the attribute → component mapping.
    /// Extracted from VehicleEffectRouter.ResolveModifierTarget.
    /// Used by effects that need to route to the correct component when targeting a Vehicle.
    /// </summary>
    public static class VehicleComponentResolver
    {
        public static VehicleComponent ResolveForAttribute(Vehicle vehicle, EntityAttribute attribute)
        {
            return attribute switch
            {
                EntityAttribute.MaxHealth => vehicle.chassis,
                EntityAttribute.ArmorClass => vehicle.chassis,
                EntityAttribute.MagicResistance => vehicle.chassis,
                EntityAttribute.Mobility => vehicle.chassis,
                EntityAttribute.DragCoefficient => vehicle.chassis,
                EntityAttribute.MaxEnergy => vehicle.powerCore,
                EntityAttribute.EnergyRegen => vehicle.powerCore,
                EntityAttribute.MaxSpeed => vehicle.AllComponents.OfType<DriveComponent>().FirstOrDefault(),
                EntityAttribute.Acceleration => vehicle.AllComponents.OfType<DriveComponent>().FirstOrDefault(),
                EntityAttribute.Stability => vehicle.AllComponents.OfType<DriveComponent>().FirstOrDefault(),
                EntityAttribute.BaseFriction => vehicle.AllComponents.OfType<DriveComponent>().FirstOrDefault(),
                _ => vehicle.chassis
            };
        }
    }
}
