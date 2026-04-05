using Assets.Scripts.Conditions;
using Assets.Scripts.Conditions.CharacterConditions;
using Assets.Scripts.Entities.Vehicles;
using SerializeReferenceEditor;
using UnityEngine;

namespace Assets.Scripts.Effects.EffectTypes.SeatEffects
{
    /// <summary>
    /// Removes character conditions from the operator of the targeted component or seat.
    /// Mirrors ApplyCharacterConditionEffect for target resolution.
    /// For removing entity conditions (buffs/debuffs on components), use RemoveEntityConditionEffect instead.
    /// </summary>
    [System.Serializable]
    [SRName("Remove Character Condition")]
    public class RemoveCharacterConditionEffect : ISeatEffect
    {
        [Header("Removal Filter")]
        [Tooltip("Categories to remove (e.g., DoT removes all burning/bleeding). Leave None to use specific template.")]
        public ConditionCategory categoriesToRemove = ConditionCategory.None;

        [Tooltip("Optional: remove only this specific condition template. Takes priority over categories.")]
        public CharacterCondition specificTemplate;

        void ISeatEffect.Apply(VehicleSeat target, EffectContext context)
        {
            if (!target.IsAssigned) return;

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
                Debug.LogWarning("[RemoveCharacterConditionEffect] Neither specificTemplate nor categoriesToRemove set!");
            }
        }
    }
}
