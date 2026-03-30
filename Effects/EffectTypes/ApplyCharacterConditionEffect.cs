using Assets.Scripts.Conditions.CharacterConditions;
using Assets.Scripts.Entities;
using Assets.Scripts.Entities.Vehicles;
using Assets.Scripts.Entities.Vehicles.VehicleComponents;
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

        private VehicleSeat ResolveSeat(IEffectTarget target)
        {
            switch (target)
            {
                case VehicleSeat seat:
                    return seat;
                case VehicleComponent component:
                    return ResolveSeatFromComponent(component);
                case Vehicle:
                    Debug.LogWarning($"[{GetType().Name}] Cannot apply to a Vehicle — which crew member? Use a component or VehicleSeat target.");
                    return null;
                default:
                    string typeName = target != null ? target.GetType().Name : "null";
                    Debug.LogWarning($"[{GetType().Name}] Unsupported target type: {typeName}. Use a component or VehicleSeat target.");
                    return null;
            }
        }

        private VehicleSeat ResolveSeatFromComponent(VehicleComponent component)
        {
            Vehicle vehicle = component.ParentVehicle;
            if (vehicle == null)
            {
                Debug.LogWarning($"[{GetType().Name}] Component '{component.name}' has no parent vehicle.");
                return null;
            }

            return vehicle.GetSeatForComponent(component);
        }
    }
}
