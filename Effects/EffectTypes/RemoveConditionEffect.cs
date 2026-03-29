using Assets.Scripts.Conditions;
using Assets.Scripts.Conditions.EntityConditions;
using Assets.Scripts.Entities;
using SerializeReferenceEditor;
using UnityEngine;

namespace Assets.Scripts.Effects.EffectTypes
{
    /// <summary>
    /// Removes status effects from a target entity by category or specific template.
    /// Used for dispel skills (e.g., Extinguish removes DoT) and narrative events (e.g., cure SuperAids).
    /// </summary>
    [System.Serializable]
    [SRName("Remove Condition")]
    public class RemoveStatusEffect : EffectBase
    {
        [Header("Removal Filter")]
        [Tooltip("Categories to remove (e.g., DoT removes all burning/bleeding). Leave None to use specific template.")]
        public ConditionCategory categoriesToRemove = ConditionCategory.None;

        [Tooltip("Optional: remove only this specific status effect template. Takes priority over categories.")]
        public EntityCondition specificTemplate;

        public override void Apply(Entity target, EffectContext context)
        {
            if (specificTemplate != null)
            {
                target.RemoveConditionsByTemplate(specificTemplate);
            }
            else if (categoriesToRemove != ConditionCategory.None)
            {
                target.RemoveConditionsByCategory(categoriesToRemove);
            }
            else
            {
                Debug.LogWarning("[RemoveStatusEffectsEffect] Neither specificTemplate nor categoriesToRemove set!");
            }
        }
    }
}
