/// <summary>
/// Static utility class for common Entity-related helper methods.
/// Used by effects, invocations, and other systems that work with Entities.
/// </summary>
public static class EntityHelpers
{
    /// <summary>
    /// Get the parent vehicle for an entity.
    /// If entity is a VehicleComponent, return its parent vehicle.
    /// Returns null if entity is not a vehicle component.
    /// </summary>
    public static Vehicle GetParentVehicle(Entity entity)
    {
        if (entity is VehicleComponent component)
        {
            return component.ParentVehicle;
        }
        return null;
    }

    /// <summary>
    /// Get a display name for any entity.
    /// For VehicleComponents, includes the parent vehicle name.
    /// For other entities, returns their display name.
    /// </summary>
    public static string GetEntityDisplayName(Entity entity)
    {
        if (entity == null)
            return "Unknown";

        if (entity is VehicleComponent component)
        {
            return component.ParentVehicle != null
                ? $"{component.ParentVehicle.vehicleName}'s {component.name}"
                : component.name;
        }
        return entity.GetDisplayName();
    }
}
