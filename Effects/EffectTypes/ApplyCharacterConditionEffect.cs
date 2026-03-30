using Assets.Scripts.Conditions.CharacterConditions;
using Assets.Scripts.Entities.Vehicles;
using SerializeReferenceEditor;
using UnityEngine;

namespace Assets.Scripts.Effects.EffectTypes
{
    /// <summary>
    /// Applies a character condition to the operator of the targeted component.
    /// Effects always target components (the physical world). This effect navigates
    /// the component → vehicle → seat chain to reach the character sitting at that station.
    /// If the component has no assigned operator the effect is silently skipped —
    /// expected for uncrewed or AI-only seats.
    /// </summary>
    [System.Serializable]
    [SRName("Character Condition")]
    public class ApplyCharacterConditionEffect : EffectBase
    {
        [Header("Character Condition")]
        [Tooltip("The CharacterCondition asset to apply to the operator of the targeted component")]
        public CharacterCondition condition;

        public override void Apply(IEffectTarget target, EffectContext context)
        {
            if (condition == null)
            {
                Debug.LogWarning("[ApplyCharacterConditionEffect] No condition assigned!");
                return;
            }

            VehicleSeat seat = ResolveSeat(target);
            if (seat == null || !seat.IsAssigned) return;

            seat.ApplyCondition(condition, context.SourceActor?.GetEntity());
        }
    }
}
