using System;
using UnityEngine;
using Assets.Scripts.Effects;

/// <summary>
/// Permanent invisible stat modifier — no duration, no status icon.
/// For temporary buffs/debuffs, use ApplyStatusEffect.
/// Per ModifierSystem.md: skills should generally use StatusEffects, not this.
/// Probably deprecated in favor of the status effect system but I'm keeping it just in case.
/// </summary>
[Serializable]
public class AttributeModifierEffect : EffectBase
{
    [Header("Modifier Configuration")]
    public Attribute attribute;
    public ModifierType type;
    public float value;

    public AttributeModifier ToRuntimeModifier(UnityEngine.Object source)
    {
        return new AttributeModifier(
            attribute,
            type,
            value,
            source,
            ModifierCategory.Other
        );
    }

    public override void Apply(Entity user, Entity target, EffectContext context, UnityEngine.Object source = null)
    {
        target.AddModifier(ToRuntimeModifier(source));
    }
}
