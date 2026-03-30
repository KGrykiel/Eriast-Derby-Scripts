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
        public static VehicleComponent ResolveForAttribute(Vehicle vehicle, Attribute attribute)
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
                Attribute.MaxSpeed => vehicle.AllComponents.OfType<DriveComponent>().FirstOrDefault(),
                Attribute.Acceleration => vehicle.AllComponents.OfType<DriveComponent>().FirstOrDefault(),
                Attribute.Stability => vehicle.AllComponents.OfType<DriveComponent>().FirstOrDefault(),
                Attribute.BaseFriction => vehicle.AllComponents.OfType<DriveComponent>().FirstOrDefault(),
                _ => vehicle.chassis
            };
        }
    }
}
