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
            
            // Old logging (keep for backwards compatibility)
            SimulationLogger.LogEvent($"{vehicle.vehicleName} receives a modifier: {type} {attribute} {value} for {durationTurns} turns.");
    
            // Note: Detailed modifier logging is already handled by Vehicle.AddModifier()
            // which includes duration, source, and attribute details.
            // We log here only the raw effect application for debugging purposes.
            
            string sourceText = actualSource != null ? actualSource.name : "unknown source";
            string durText = durationTurns < 0 ? "permanent" : $"{durationTurns} turns";
            bool isPositive = (type == ModifierType.Flat && value > 0) || (type == ModifierType.Percent && value > 0);
          
            RaceHistory.Log(
                RacingGame.Events.EventType.Modifier,
                EventImportance.Debug,
                $"[BUFF] {sourceText} applied {type} {attribute} {value:+0;-0} to {vehicle.vehicleName} ({durText})",
                vehicle.currentStage,
                vehicle
            ).WithMetadata("modifierType", type.ToString())
             .WithMetadata("attribute", attribute.ToString())
             .WithMetadata("value", value)
             .WithMetadata("duration", durationTurns)
             .WithMetadata("source", sourceText)
             .WithMetadata("local", local)
             .WithMetadata("isPositive", isPositive);
        }
        // Handle other Entity types here in the future
    }
}
