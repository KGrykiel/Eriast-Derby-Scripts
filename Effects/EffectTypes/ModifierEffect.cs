using SerializeReferenceEditor;
using System;
using UnityEngine;

namespace Assets.Scripts.Effects.EffectTypes
{
    /// <summary>
    /// Permanent invisible stat modifier — no duration, no status icon.
    /// For temporary buffs/debuffs, use ApplyStatusEffect.
    /// Per ModifierSystem.md: skills should generally use StatusEffects, not this.
    /// Probably deprecated in favor of the status effect system but I'm keeping it just in case.
    /// </summary>
    [Serializable]
    [SRName("Modifier")]
    public class AttributeModifierEffect : EffectBase
    {
        [Header("Modifier Configuration")]
        public Attribute attribute;
        public ModifierType type;
        public float value;

        [Tooltip("Label shown in logs and tooltips for this modifier")]
        public string label = "Effect Modifier";

        public AttributeModifier ToRuntimeModifier()
        {
            return new AttributeModifier(
                attribute,
                type,
                value,
                string.IsNullOrEmpty(label) ? "Effect Modifier" : label
            );
        }

        public override void Apply(Entity target, EffectContext context)
        {
            target.AddModifier(ToRuntimeModifier());
        }
    }
}
