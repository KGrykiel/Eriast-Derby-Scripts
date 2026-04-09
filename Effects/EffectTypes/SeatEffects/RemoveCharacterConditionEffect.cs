using System;
using Assets.Scripts.Conditions;
using Assets.Scripts.Conditions.CharacterConditions;
using Assets.Scripts.Entities.Vehicles;
using SerializeReferenceEditor;
using UnityEngine;

namespace Assets.Scripts.Effects.EffectTypes.SeatEffects
{
    /// <summary>
    /// Removes character conditions from the operator of the targeted seat by category.
    /// For removing entity conditions (buffs/debuffs on components), use RemoveEntityConditionEffect instead.
    /// </summary>
    [Serializable]
    [SRName("Remove Character Condition/By Category")]
    public class RemoveCharacterConditionByCategoryEffect : ISeatEffect
    {
        [Tooltip("Categories to remove (e.g., DoT removes all burning/bleeding).")]
        public ConditionCategory categoriesToRemove = ConditionCategory.None;

        void ISeatEffect.Apply(VehicleSeat target, EffectContext context)
        {
            if (!target.IsAssigned) return;

            if (categoriesToRemove == ConditionCategory.None)
            {
                Debug.LogWarning("[RemoveCharacterConditionByCategoryEffect] No categories set — effect had no impact.");
                return;
            }

            target.RemoveConditionsByCategory(categoriesToRemove);
        }
    }

    /// <summary>
    /// Removes a specific character condition template from the operator of the targeted seat.
    /// For removing entity conditions (buffs/debuffs on components), use RemoveEntityConditionEffect instead.
    /// </summary>
    [Serializable]
    [SRName("Remove Character Condition/By Template")]
    public class RemoveCharacterConditionByTemplateEffect : ISeatEffect
    {
        [Tooltip("Specific condition template to remove.")]
        public CharacterCondition template;

        void ISeatEffect.Apply(VehicleSeat target, EffectContext context)
        {
            if (!target.IsAssigned) return;

            if (template == null)
            {
                Debug.LogWarning("[RemoveCharacterConditionByTemplateEffect] No template assigned — effect had no impact.");
                return;
            }

            target.RemoveConditionsByTemplate(template);
        }
    }
}
