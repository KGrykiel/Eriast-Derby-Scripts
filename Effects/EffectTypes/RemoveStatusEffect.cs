using UnityEngine;
using Assets.Scripts.StatusEffects;
using StatusEffectTemplate = Assets.Scripts.StatusEffects.StatusEffect;

namespace Assets.Scripts.Effects.EffectTypes
{
    /// <summary>
    /// Removes status effects from a target entity by category or specific template.
    /// Used for dispel skills (e.g., Extinguish removes DoT) and narrative events (e.g., cure SuperAids).
    /// </summary>
    [System.Serializable]
    public class RemoveStatusEffect : EffectBase
    {
        [Header("Removal Filter")]
        [Tooltip("Categories to remove (e.g., DoT removes all burning/bleeding). Leave None to use specific template.")]
        public EffectCategory categoriesToRemove = EffectCategory.None;

        [Tooltip("Optional: remove only this specific status effect template. Takes priority over categories.")]
        public StatusEffectTemplate specificTemplate;

        public override void Apply(Entity target, EffectContext context)
        {
            if (specificTemplate != null)
            {
                target.RemoveStatusEffectsByTemplate(specificTemplate);
            }
            else if (categoriesToRemove != EffectCategory.None)
            {
                target.RemoveStatusEffectsByCategory(categoriesToRemove);
            }
            else
            {
                Debug.LogWarning("[RemoveStatusEffectsEffect] Neither specificTemplate nor categoriesToRemove set!");
            }
        }
    }
}
