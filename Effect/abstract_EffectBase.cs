using UnityEngine;

/// <summary>
/// Abstract base class for all effects.
/// Provides common helper methods for working with Entities and VehicleComponents.
/// </summary>
[System.Serializable]
public abstract class EffectBase : IEffect
{
    public abstract void Apply(Entity user, Entity target, Object context = null, Object source = null);
    
    // ==================== HELPER METHODS (Delegates to EntityHelpers) ====================
    
    /// <summary>
    /// Get the parent vehicle for an entity.
    /// If entity is a VehicleComponent, return its parent vehicle.
    /// Returns null if entity is not a vehicle component.
    /// </summary>
    protected static Vehicle GetParentVehicle(Entity entity)
    {
        return EntityHelpers.GetParentVehicle(entity);
    }
    
    /// <summary>
    /// Get a display name for any entity.
    /// For VehicleComponents, includes the parent vehicle name.
    /// </summary>
    protected static string GetEntityDisplayName(Entity entity)
    {
        return EntityHelpers.GetEntityDisplayName(entity);
    }
}
