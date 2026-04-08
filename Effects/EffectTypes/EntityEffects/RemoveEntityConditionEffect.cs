using Assets.Scripts.Conditions;
using Assets.Scripts.Conditions.EntityConditions;
using Assets.Scripts.Entities;
using SerializeReferenceEditor;
using UnityEngine;

namespace Assets.Scripts.Effects.EffectTypes.EntityEffects
{
    /// <summary>
    /// Removes entity conditions from a specific component target by category or specific template.
    /// Use in EntityEffectInvocation. For vehicle-wide conditions use RemoveVehicleConditionEffect instead.
    /// </summary>
    [System.Serializable]
    [SRName("Remove Condition")]
    public class RemoveEntityConditionEffect : IEntityEffect
    {
        [Header("Removal Filter")]
        [Tooltip("Categories to remove (e.g., DoT removes all burning/bleeding). Leave None to use specific template.")]
        public ConditionCategory categoriesToRemove = ConditionCategory.None;

        [Tooltip("Optional: remove only this specific status effect template. Takes priority over categories.")]
        public EntityCondition specificTemplate;

        void IEntityEffect.Apply(Entity target, EffectContext context)
        {
            if (specificTemplate != null)
                target.RemoveConditionsByTemplate(specificTemplate);
            else if (categoriesToRemove != ConditionCategory.None)
                target.RemoveConditionsByCategory(categoriesToRemove);
            else
                Debug.LogWarning("[RemoveEntityConditionEffect] Neither specificTemplate nor categoriesToRemove set!");
        }
    }
}
