using System;
using UnityEngine;
using RacingGame.Events;

[Serializable]
public class AttributeModifierEffect : EffectBase
{
    public Attribute attribute;
    public ModifierType type;
    public float value;
    [Tooltip("-1 for permanent, otherwise number of turns")]
    public int durationTurns = -1;
    public bool local = false; // If true, this modifier is only applied locally

    // Converts to a runtime AttributeModifier, tagging it with the source that applied it
    public AttributeModifier ToRuntimeModifier(UnityEngine.Object source)
    {
        return new AttributeModifier(
            attribute,
            type,
            value,
            durationTurns,
            source,
            local
        );
    }

    public override void Apply(Entity user, Entity target, UnityEngine.Object context = null, UnityEngine.Object source = null)
    {
        var vehicle = target as Vehicle;
        if (vehicle != null)
        {
            // Always pass a UnityEngine.Object as source, fallback to context if available, otherwise null
            UnityEngine.Object actualSource = source ?? context;
            vehicle.AddModifier(ToRuntimeModifier(actualSource));
            
            // Logging removed - Vehicle.AddModifier() already logs with full context
            // This prevents duplicate modifier events
        }
        // Handle other Entity types here in the future
    }
}
