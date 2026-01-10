using System;
using UnityEngine;
using RacingGame.Events;

[Serializable]
public class AttributeModifierEffect : EffectBase
{
    public Attribute attribute;
    public ModifierType type;
    public float value;
    
    // NOTE: Duration and 'local' fields removed - will be handled by StatusEffect system in Phase 2
    // For now, modifiers applied by this effect are permanent (equipment-style)
    // Skills will use StatusEffect system instead (Phase 2 migration)

    // Converts to a runtime AttributeModifier, tagging it with the source that applied it
    public AttributeModifier ToRuntimeModifier(UnityEngine.Object source)
    {
        return new AttributeModifier(
            attribute,
            type,
            value,
            source
        );
    }

    /// <summary>
    /// Applies the modifier to the target entity.
    /// 
    /// NOTE: Target routing is handled by Skill.Use() before this is called.
    /// The target will already be the correct component (Drive for Speed, PowerCore for Energy, etc.)
    /// This method simply applies the modifier to whatever component it receives.
    /// 
    /// Parameter convention from Skill.Use():
    /// - target: Already-routed component (correct target after Vehicle.RouteEffectTarget)
    /// - context: WeaponComponent (for damage calculations) or null
    /// - source: Skill that triggered this effect (for modifier tracking)
    /// </summary>
    public override void Apply(Entity user, Entity target, UnityEngine.Object context = null, UnityEngine.Object source = null)
    {
        // Source should be the skill that applied this effect
        UnityEngine.Object actualSource = source ?? context;
        
        // Target should already be routed to the correct component by Skill.Use()
        // Just apply the modifier directly
        if (target is VehicleComponent targetComponent)
        {
            targetComponent.AddModifier(ToRuntimeModifier(actualSource));
            return;
        }
        
        // Fallback: If target is not a component, try routing through vehicle
        // (This shouldn't happen with new routing, but kept for safety)
        Vehicle vehicle = GetParentVehicle(target);
        if (vehicle != null)
        {
            VehicleComponent component = vehicle.ResolveModifierTarget(attribute);
            if (component != null)
            {
                component.AddModifier(ToRuntimeModifier(actualSource));
            }
        }
    }
}
