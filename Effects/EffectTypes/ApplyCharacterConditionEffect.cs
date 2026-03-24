using Assets.Scripts.Conditions.CharacterConditions;
using Assets.Scripts.Entities.Vehicle;
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

        public override void Apply(Entity target, EffectContext context)
        {
            if (condition == null)
            {
                Debug.LogWarning("[ApplyCharacterConditionEffect] No condition assigned!");
                return;
            }

            if (target is not VehicleComponent component)
            {
                Debug.LogWarning($"[ApplyCharacterConditionEffect] Target '{target.name}' is not a VehicleComponent. Character conditions require a component target.");
                return;
            }

            Vehicle vehicle = component.ParentVehicle;
            if (vehicle == null)
            {
                Debug.LogWarning($"[ApplyCharacterConditionEffect] Component '{component.name}' has no parent vehicle.");
                return;
            }

            VehicleSeat seat = vehicle.GetSeatForComponent(component);
            if (seat == null || !seat.IsAssigned)
            {
                // Uncrewed seat — no operator to apply the condition to, skip silently.
                return;
            }

            seat.ApplyCondition(condition, context.SourceEntity);
        }
    }
}
