using Assets.Scripts.Entities;
using Assets.Scripts.Entities.Vehicles;
using Assets.Scripts.Entities.Vehicles.VehicleComponents;

public static class EntityHelpers
{
    /// <summary>Returns null if entity is not a VehicleComponent.</summary>
    public static Vehicle GetParentVehicle(Entity entity)
    {
        if (entity is VehicleComponent component)
        {
            return component.ParentVehicle;
        }
        return null;
    }

    /// <summary>Resolves a <see cref="Vehicle"/> from an <see cref="IRollTarget"/>. Returns null if the target cannot be mapped to a vehicle.</summary>
    public static Vehicle GetVehicleFromTarget(IRollTarget target)
    {
        return target switch
        {
            Vehicle v        => v,
            Entity entity    => GetParentVehicle(entity),
            VehicleSeat seat => seat.ParentVehicle,
            _                => null
        };
    }
}
