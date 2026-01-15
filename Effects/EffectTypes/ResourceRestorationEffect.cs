using System;
using UnityEngine;
using Combat;

/// <summary>
/// Universal resource restoration/drain effect.
/// Restores or drains Health (chassis HP) or Energy (power core energy).
/// 
/// This effect is STATELESS - emits RestorationEvent for logging.
/// 
/// IMPORTANT: This effect should target the CHASSIS for health or POWER CORE for energy.
/// The Vehicle.RouteEffectTarget() method handles routing if targeting the vehicle.
/// </summary>
[System.Serializable]
public class ResourceRestorationEffect : EffectBase
{
    public enum ResourceType
    {
        Health,   // Restores/drains chassis HP
        Energy    // Restores/drains power core energy
    }

    [Header("Restoration Configuration")]
    [Tooltip("Which resource to restore/drain")]
    public ResourceType resourceType = ResourceType.Health;
    
    [Tooltip("Amount to restore (positive) or drain (negative)")]
    public int amount = 0;

    /// <summary>
    /// Applies this restoration effect to the target entity.
    /// Emits RestorationEvent for logging via CombatEventBus.
    /// </summary>
    public override void Apply(Entity user, Entity target, UnityEngine.Object context = null, UnityEngine.Object source = null)
    {
        // Get parent vehicle for context
        Vehicle vehicle = GetParentVehicle(target);
        if (vehicle == null) return;
        
        // Apply restoration based on resource type
        var breakdown = resourceType switch
        {
            ResourceType.Health => ApplyHealthRestoration(vehicle),
            ResourceType.Energy => ApplyEnergyRestoration(vehicle),
            _ => new RestorationBreakdown()
        };
        
        // Store context for breakdown
        breakdown.resourceType = resourceType;
        breakdown.source = source?.name ?? "unknown";
        
        // Emit event for logging (CombatEventBus handles aggregation)
        if (breakdown.actualChange != 0)
        {
            CombatEventBus.EmitRestoration(breakdown, user, target, source);
        }
    }
    
    /// <summary>
    /// Apply health restoration to vehicle chassis.
    /// </summary>
    private RestorationBreakdown ApplyHealthRestoration(Vehicle vehicle)
    {
        if (vehicle.chassis == null)
            return new RestorationBreakdown();
        
        int oldValue = vehicle.health;
        int maxValue = vehicle.maxHealth;
        int requestedChange = amount;
        
        // Clamp to valid range
        vehicle.health = Mathf.Clamp(vehicle.health + amount, 0, maxValue);
        
        int actualChange = vehicle.health - oldValue;
        
        return new RestorationBreakdown
        {
            oldValue = oldValue,
            newValue = vehicle.health,
            maxValue = maxValue,
            requestedChange = requestedChange,
            actualChange = actualChange
        };
    }
    
    /// <summary>
    /// Apply energy restoration to vehicle power core.
    /// </summary>
    private RestorationBreakdown ApplyEnergyRestoration(Vehicle vehicle)
    {
        if (vehicle.powerCore == null)
            return new RestorationBreakdown();
        
        int oldValue = vehicle.energy;
        int maxValue = vehicle.maxEnergy;
        int requestedChange = amount;
        
        // Clamp to valid range
        vehicle.energy = Mathf.Clamp(vehicle.energy + amount, 0, maxValue);
        
        int actualChange = vehicle.energy - oldValue;
        
        return new RestorationBreakdown
        {
            oldValue = oldValue,
            newValue = vehicle.energy,
            maxValue = maxValue,
            requestedChange = requestedChange,
            actualChange = actualChange
        };
    }
    
    /// <summary>
    /// Get a description of this restoration for UI/logging.
    /// </summary>
    public string GetRestorationDescription()
    {
        string action = amount > 0 ? "restore" : "drain";
        string resource = resourceType.ToString();
        return $"{action} {Mathf.Abs(amount)} {resource}";
    }
}

/// <summary>
/// Tracks the breakdown of a resource restoration/drain operation.
/// </summary>
[Serializable]
public class RestorationBreakdown
{
    public ResourceRestorationEffect.ResourceType resourceType;
    public int oldValue;
    public int newValue;
    public int maxValue;
    public int requestedChange;
    public int actualChange;
    public string source;
    
    public float NewPercentage => maxValue > 0 ? (float)newValue / maxValue : 0f;
    public bool WasClamped => requestedChange != actualChange;
    
    public string ToFormattedString()
    {
        if (actualChange == 0)
            return $"No change ({newValue}/{maxValue})";
        
        string action = actualChange > 0 ? "restored" : "drained";
        string clampedText = WasClamped ? " (clamped)" : "";
        
        return $"{action} {Mathf.Abs(actualChange)} {resourceType} ({newValue}/{maxValue}){clampedText}";
    }
}
