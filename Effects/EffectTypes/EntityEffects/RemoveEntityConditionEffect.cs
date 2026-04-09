using System;
using Assets.Scripts.Conditions;
using Assets.Scripts.Conditions.EntityConditions;
using Assets.Scripts.Entities;
using SerializeReferenceEditor;
using UnityEngine;

namespace Assets.Scripts.Effects.EffectTypes.EntityEffects
{
    /// <summary>
    /// Removes entity conditions from the target by category.
    /// Use in EntityEffectInvocation. For vehicle-wide conditions use RemoveVehicleConditionEffect instead.
    /// </summary>
    [Serializable]
    [SRName("Remove Condition/By Category")]
    public class RemoveEntityConditionByCategoryEffect : IEntityEffect
    {
        [Tooltip("Categories to remove (e.g., DoT removes all burning/bleeding).")]
        public ConditionCategory categoriesToRemove = ConditionCategory.None;

        void IEntityEffect.Apply(Entity target, EffectContext context)
        {
            if (categoriesToRemove == ConditionCategory.None)
            {
                Debug.LogWarning("[RemoveEntityConditionByCategoryEffect] No categories set — effect had no impact.");
                return;
            }

            target.RemoveConditionsByCategory(categoriesToRemove);
        }
    }

    /// <summary>
    /// Removes a specific entity condition template from the target.
    /// Use in EntityEffectInvocation. For vehicle-wide conditions use RemoveVehicleConditionEffect instead.
    /// </summary>
    [Serializable]
    [SRName("Remove Condition/By Template")]
    public class RemoveEntityConditionByTemplateEffect : IEntityEffect
    {
        [Tooltip("Specific condition template to remove.")]
        public EntityCondition template;

        void IEntityEffect.Apply(Entity target, EffectContext context)
        {
            if (template == null)
            {
                Debug.LogWarning("[RemoveEntityConditionByTemplateEffect] No template assigned — effect had no impact.");
                return;
            }

            target.RemoveConditionsByTemplate(template);
        }
    }
}
