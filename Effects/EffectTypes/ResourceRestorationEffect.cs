using UnityEngine;
using Assets.Scripts.Entities.Vehicle.VehicleComponents.ComponentTypes;
using Assets.Scripts.Effects;
using Assets.Scripts.Combat;

/// <summary>
/// Restores or drains Health/Energy. Works directly on the target entity passed — no routing.
/// Right now uses a simple flat amount, but should probably use dice like damage. It's the DnD way, after all.
/// </summary>
[System.Serializable]
public class ResourceRestorationEffect : EffectBase
{
    public enum ResourceType
    {
        Health,
        Energy
    }

    [Header("Restoration Configuration")]
    [Tooltip("Which resource to restore/drain")]
    public ResourceType resourceType = ResourceType.Health;

    [Tooltip("Amount to restore (positive) or drain (negative)")]
    public int amount = 0;

    public override void Apply(Entity target, EffectContext context, UnityEngine.Object source = null)
    {
        var breakdown = resourceType switch
        {
            ResourceType.Health => ApplyHealthRestoration(target),
            ResourceType.Energy => ApplyEnergyRestoration(target),
            _ => new RestorationBreakdown()
        };

        breakdown.resourceType = resourceType;
        breakdown.source = source != null ? source.name : null ?? "unknown";

        if (breakdown.actualChange != 0)
        {
            CombatEventBus.EmitRestoration(breakdown, context.SourceEntity, target, source);
        }
    }

    private RestorationBreakdown ApplyHealthRestoration(Entity target)
    {
        int oldValue = target.GetCurrentHealth();
        int maxValue = target.GetMaxHealth();
        int requestedChange = amount;

        if (amount >= 0)
            target.Heal(amount);

        int actualChange = target.GetCurrentHealth() - oldValue;

        return new RestorationBreakdown
        {
            oldValue = oldValue,
            newValue = target.GetCurrentHealth(),
            maxValue = maxValue,
            requestedChange = requestedChange,
            actualChange = actualChange
        };
    }
    
    private RestorationBreakdown ApplyEnergyRestoration(Entity target)
    {
        if (target is PowerCoreComponent powerCore)
        {
            int oldValue = powerCore.currentEnergy;
            int maxValue = powerCore.GetMaxEnergy();
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
}

public class RestorationBreakdown
{
    public ResourceRestorationEffect.ResourceType resourceType;
    public int oldValue;
    public int newValue;
    public int maxValue;
    public int requestedChange;
    public int actualChange;
    public string source;
}


