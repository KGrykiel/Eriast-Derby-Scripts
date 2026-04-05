using Assets.Scripts.Conditions.CharacterConditions;
using Assets.Scripts.Entities.Vehicles;
using SerializeReferenceEditor;
using UnityEngine;

namespace Assets.Scripts.Effects.EffectTypes.SeatEffects
{
    /// <summary>
    /// Applies a character condition to the operator of the targeted seat.
    /// Use TargetSeatResolver to resolve a VehicleComponent to its controlling seat.
    /// If the seat has no assigned operator the effect is silently skipped —
    /// expected for uncrewed or AI-only seats.
    /// </summary>
    [System.Serializable]
    [SRName("Character Condition")]
    public class ApplyCharacterConditionEffect : ISeatEffect
    {
        [Header("Character Condition")]
        [Tooltip("The CharacterCondition asset to apply to the operator of the targeted component")]
        public CharacterCondition condition;

        void ISeatEffect.Apply(VehicleSeat target, EffectContext context)
        {
            if (condition == null)
            {
                Debug.LogWarning("[ApplyCharacterConditionEffect] No condition assigned!");
                return;
            }

            if (!target.IsAssigned) return;

            target.ApplyCondition(condition, context.SourceActor?.GetEntity());
        }
    }
}
