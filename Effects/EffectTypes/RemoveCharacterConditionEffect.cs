using Assets.Scripts.Conditions;
using Assets.Scripts.Conditions.CharacterConditions;
using Assets.Scripts.Entities.Vehicles;
using SerializeReferenceEditor;
using UnityEngine;

namespace Assets.Scripts.Effects.EffectTypes
{
    /// <summary>
    /// Removes character conditions from the operator of the targeted component or seat.
    /// Mirrors ApplyCharacterConditionEffect for target resolution.
    /// For removing entity conditions (buffs/debuffs on components), use RemoveEntityConditionEffect instead.
    /// </summary>
    [System.Serializable]
    [SRName("Remove Character Condition")]
    public class RemoveCharacterConditionEffect : EffectBase
    {
        [Header("Removal Filter")]
        [Tooltip("Categories to remove (e.g., DoT removes all burning/bleeding). Leave None to use specific template.")]
        public ConditionCategory categoriesToRemove = ConditionCategory.None;

        [Tooltip("Optional: remove only this specific condition template. Takes priority over categories.")]
        public CharacterCondition specificTemplate;

        public override void Apply(IEffectTarget target, EffectContext context)
        {
            VehicleSeat seat = ResolveSeat(target);
            if (seat == null || !seat.IsAssigned) return;

            if (specificTemplate != null)
            {
                seat.RemoveConditionsByTemplate(specificTemplate);
            }
            else if (categoriesToRemove != ConditionCategory.None)
            {
                seat.RemoveConditionsByCategory(categoriesToRemove);
            }
            else
            {
                Debug.LogWarning("[RemoveCharacterConditionEffect] Neither specificTemplate nor categoriesToRemove set!");
            }
        }
    }
}
