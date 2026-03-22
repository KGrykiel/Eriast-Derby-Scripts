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

    public AttributeModifier ToRuntimeModifier()
    {
        return new AttributeModifier(
            attribute,
            type,
            value,
            null,
            ModifierCategory.Other
        );
    }

    public override void Apply(Entity target, EffectContext context)
    {
        target.AddModifier(ToRuntimeModifier());
    }
}
