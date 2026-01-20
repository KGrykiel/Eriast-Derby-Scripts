using System;
using UnityEngine;
using Assets.Scripts.Entities.Vehicle.VehicleComponents.ComponentTypes;
using Assets.Scripts.Effects;
using Assets.Scripts.Combat;

/// <summary>
/// Universal resource restoration/drain effect.
/// Restores or drains Health or Energy from entities.
/// 
/// This effect is STATELESS - emits RestorationEvent for logging.
/// 
/// DESIGN: This effect works DIRECTLY on the target entity passed to it. No routing!
/// - Health: Restores entity.health
/// - Energy: Restores entity.energy (if entity has energy property)
/// 
/// All routing logic is handled by SkillEffectApplicator.
/// This effect is dumb - it just modifies whatever entity you give it.
/// </summary>
[System.Serializable]
public class ResourceRestorationEffect : EffectBase
{
    public enum ResourceType
    {
        Health,   // Restores/drains entity HP
        Energy    // Restores/drains entity energy
    }

    [Header("Restoration Configuration")]
    [Tooltip("Which resource to restore/drain")]
    public ResourceType resourceType = ResourceType.Health;
    
    [Tooltip("Amount to restore (positive) or drain (negative)")]
    public int amount = 0;

    /// <summary>
    /// Applies this restoration effect to the target entity.
    /// Emits RestorationEvent for logging via CombatEventBus.
    /// Works ONLY on the entity passed - no routing!
    /// </summary>
    public override void Apply(Entity user, Entity target, EffectContext? context = null, UnityEngine.Object source = null)
    {
        if (target == null) return;
        
        // Apply restoration directly to target entity
        var breakdown = resourceType switch
        {
            ResourceType.Health => ApplyHealthRestoration(target),
            ResourceType.Energy => ApplyEnergyRestoration(target),
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
    /// Apply health restoration directly to entity.
    /// No routing - just restores entity.health.
    /// </summary>
    private RestorationBreakdown ApplyHealthRestoration(Entity target)
    {
        int oldValue = target.health;
        int maxValue = target.maxHealth;
        int requestedChange = amount;
        
        // Clamp to valid range
        target.health = Mathf.Clamp(target.health + amount, 0, maxValue);
        
        int actualChange = target.health - oldValue;
        
        return new RestorationBreakdown
        {
            oldValue = oldValue,
            newValue = target.health,
            maxValue = maxValue,
            requestedChange = requestedChange,
            actualChange = actualChange
        };
    }
    
    /// <summary>
    /// Apply energy restoration directly to entity.
    /// Works on PowerCoreComponent (component.currentEnergy).
    /// </summary>
    private RestorationBreakdown ApplyEnergyRestoration(Entity target)
    {
        // PowerCoreComponent stores energy
        if (target is PowerCoreComponent powerCore)
        {
            int oldValue = powerCore.currentEnergy;
            int maxValue = powerCore.maxEnergy;
            int requestedChange = amount;
            
            // Clamp to valid range
            powerCore.currentEnergy = Mathf.Clamp(powerCore.currentEnergy + amount, 0, maxValue);
            
            int actualChange = powerCore.currentEnergy - oldValue;
            
            return new RestorationBreakdown
            {
                oldValue = oldValue,
                newValue = powerCore.currentEnergy,
                maxValue = maxValue,
                requestedChange = requestedChange,
                actualChange = actualChange
            };
        }
        
        Debug.LogWarning($"[ResourceRestorationEffect] Energy restoration requires PowerCoreComponent target. Got: {target.GetType().Name}");
        return new RestorationBreakdown();
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


