using System;
using UnityEngine;

/// <summary>
/// Universal resource restoration/drain effect.
/// Restores or drains Health (chassis HP) or Energy (power core energy).
/// 
/// IMPORTANT: This effect should target the CHASSIS for health or POWER CORE for energy.
/// The Vehicle.RouteEffectTarget() method handles routing if targeting the vehicle.
/// 
/// Like DamageEffect, this effect doesn't log directly - Skill.cs handles logging.
/// This allows consistent multi-effect skill logging (e.g., "heals 15 HP and restores 10 energy").
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
    
    // Store last restoration breakdown for retrieval by Skill.Use()
    private RestorationBreakdown lastBreakdown;
    
    /// <summary>
    /// Gets the actual amount restored in the last application.
    /// </summary>
    public int LastAmountRestored => lastBreakdown?.actualChange ?? 0;
    
    /// <summary>
    /// Gets the full breakdown of the last restoration.
    /// </summary>
    public RestorationBreakdown LastBreakdown => lastBreakdown;
    
    /// <summary>
    /// Gets whether the last effect was a restoration (positive) or drain (negative).
    /// </summary>
    public bool WasRestoration => lastBreakdown?.actualChange > 0;

    /// <summary>
    /// Applies this restoration effect to the target entity.
    /// 
    /// NOTE: For proper component targeting:
    /// - Health restoration should target ChassisComponent (has health field)
    /// - Energy restoration should target PowerCoreComponent (has energy field)
    /// 
    /// Vehicle.RouteEffectTarget() handles routing if skill targets the vehicle.
    /// 
    /// Parameter convention from Skill.Use():
    /// - target: Already-routed Entity (chassis for health, power core for energy)
    /// - source: Skill that triggered this effect (for tracking)
    /// </summary>
    public override void Apply(Entity user, Entity target, UnityEngine.Object context = null, UnityEngine.Object source = null)
    {
        // Get parent vehicle for context
        Vehicle vehicle = GetParentVehicle(target);
        if (vehicle == null) return;
        
        // Apply restoration based on resource type
        lastBreakdown = resourceType switch
        {
            ResourceType.Health => ApplyHealthRestoration(vehicle),
            ResourceType.Energy => ApplyEnergyRestoration(vehicle),
            _ => new RestorationBreakdown()
        };
        
        // Store context for breakdown
        lastBreakdown.resourceType = resourceType;
        lastBreakdown.source = source?.name ?? "unknown";
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
/// Similar to DamageBreakdown, allows Skill.cs to log detailed information.
/// </summary>
[Serializable]
public class RestorationBreakdown
{
    public ResourceRestorationEffect.ResourceType resourceType;
    public int oldValue;
    public int newValue;
    public int maxValue;
    public int requestedChange;  // What was requested (can be clamped)
    public int actualChange;     // What actually happened
    public string source;
    
    /// <summary>
    /// Get percentage of resource after restoration.
    /// </summary>
    public float NewPercentage => maxValue > 0 ? (float)newValue / maxValue : 0f;
    
    /// <summary>
    /// Was the restoration clamped by max/min limits?
    /// </summary>
    public bool WasClamped => requestedChange != actualChange;
    
    /// <summary>
    /// Get a formatted string for logging/tooltips.
    /// </summary>
    public string ToFormattedString()
    {
        if (actualChange == 0)
            return $"No change ({newValue}/{maxValue})";
        
        string action = actualChange > 0 ? "restored" : "drained";
        string clampedText = WasClamped ? " (clamped)" : "";
        
        return $"{action} {Mathf.Abs(actualChange)} {resourceType} ({newValue}/{maxValue}){clampedText}";
    }
}
