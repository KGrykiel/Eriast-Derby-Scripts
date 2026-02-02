using System;
using UnityEngine;
using Assets.Scripts.Effects;

/// <summary>
/// Applies a PERMANENT stat modifier with no duration or visual status.
/// 
/// Use Cases:
/// - EventCards applying permanent changes ("Chassis scarred: -5 max HP", "Engine upgraded: +2 Speed")
/// - Direct stat modifications that should NOT show as status effects
/// 
/// For TEMPORARY buffs/debuffs with icons and duration, use ApplyStatusEffect instead.
/// For EQUIPMENT bonuses, use Component.AddModifier() directly (no effect needed).
/// 
/// IMPORTANT: Skills should generally use ApplyStatusEffect, not this.
/// Per ModifierSystem.md Rule 1: "Skills Apply Status Effects ONLY"
/// This effect exists for edge cases where permanent, invisible modifiers are needed.
/// </summary>
[Serializable]
public class AttributeModifierEffect : EffectBase
{
    [Header("Modifier Configuration")]
    public Attribute attribute;
    public ModifierType type;
    public float value;
    
    [Header("Usage Note")]
    [Tooltip("This creates a PERMANENT modifier with no duration or status icon. For temporary buffs, use ApplyStatusEffect instead.")]
    [SerializeField, TextArea(2, 3)]
    private string usageNote = "Creates PERMANENT modifier. For temporary buffs/debuffs, use ApplyStatusEffect instead.";

    /// <summary>
    /// Converts to a runtime AttributeModifier, tagging it with the source that applied it.
    /// </summary>
    public AttributeModifier ToRuntimeModifier(UnityEngine.Object source)
    {
        return new AttributeModifier(
            attribute,
            type,
            value,
            source,
            ModifierCategory.Other  // Permanent modifiers from effects are "Other" category
        );
    }

    /// <summary>
    /// Applies a PERMANENT modifier to the target entity.
    /// 
    /// NOTE: Target routing is handled by Skill.Use() before this is called.
    /// The target will already be the correct component (Drive for Speed, PowerCore for Energy, etc.)
    /// 
    /// Parameter convention:
    /// - target: Already-routed component (correct target after Vehicle.RouteEffectTarget)
    /// - context: Combat state (unused for modifiers)
    /// - source: Skill/EventCard that triggered this (for modifier source tracking)
    /// </summary>
    public override void Apply(Entity user, Entity target, EffectContext context, UnityEngine.Object source = null)
    {
        if (target == null)
        {
            Debug.LogWarning("[AttributeModifierEffect] Target is null!");
            return;
        }
        
        // Source should be the skill/eventcard that applied this effect
        UnityEngine.Object actualSource = source;
        
        // Target should already be routed to the correct component by Skill.Use()
        // Just apply the modifier directly
        if (target is VehicleComponent targetComponent)
        {
            targetComponent.AddModifier(ToRuntimeModifier(actualSource));
            return;
        }
        
        // Fallback: If target is not a component, try routing through vehicle
        // (This shouldn't happen with new routing, but kept for safety)
        Vehicle vehicle = EntityHelpers.GetParentVehicle(target);
        if (vehicle != null)
        {
            VehicleComponent component = vehicle.ResolveModifierTarget(attribute);
            if (component != null)
            {
                component.AddModifier(ToRuntimeModifier(actualSource));
            }
        }
        else
        {
            // Direct entity (not a vehicle component) - apply directly
            target.AddModifier(ToRuntimeModifier(actualSource));
        }
    }
    
    /// <summary>
    /// Get description for UI/logging.
    /// </summary>
    public string GetDescription()
    {
        string sign = value >= 0 ? "+" : "";
        string typeStr = type == ModifierType.Multiplier ? "×" : "";
        
        if (type == ModifierType.Multiplier)
        {
            return $"{typeStr}{value} {attribute} (permanent)";
        }
        else
        {
            return $"{sign}{value} {attribute} (permanent)";
        }
    }
}
