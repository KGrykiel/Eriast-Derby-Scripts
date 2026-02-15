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
}
