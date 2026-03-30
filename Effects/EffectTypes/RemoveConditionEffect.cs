using Assets.Scripts.Conditions;
using Assets.Scripts.Conditions.EntityConditions;
using Assets.Scripts.Entities;
using Assets.Scripts.Entities.Vehicles;
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

        public override void Apply(IEffectTarget target, EffectContext context)
        {
            Entity entity = ResolveEntity(target);
            if (entity == null) return;

            if (specificTemplate != null)
            {
                entity.RemoveConditionsByTemplate(specificTemplate);
            }
            else if (categoriesToRemove != ConditionCategory.None)
            {
                entity.RemoveConditionsByCategory(categoriesToRemove);
            }
            else
            {
                Debug.LogWarning("[RemoveConditionEffect] Neither specificTemplate nor categoriesToRemove set!");
            }
        }
    }
}
