using Assets.Scripts.Conditions.VehicleConditions;
using Assets.Scripts.Entities.Vehicles;
using SerializeReferenceEditor;
using UnityEngine;

namespace Assets.Scripts.Effects.EffectTypes.VehicleEffects
{
    /// <summary>
    /// Applies a vehicle-wide condition. Always use in a VehicleEffectInvocation.
    /// For component-specific conditions use ApplyEntityConditionEffect in an EntityEffectInvocation.
    /// </summary>
    [System.Serializable]
    [SRName("Apply Vehicle Condition")]
    public class ApplyVehicleConditionEffect : IVehicleEffect
    {
        [Header("Vehicle Condition")]
        [Tooltip("The VehicleCondition asset to apply (create via Racing/Vehicle Condition menu)")]
        public VehicleCondition condition;

        void IVehicleEffect.Apply(Vehicle target, EffectContext context)
        {
            if (condition == null)
            {
                Debug.LogWarning("[ApplyVehicleConditionEffect] No condition assigned!");
                return;
            }

            target.ApplyVehicleCondition(condition, context.SourceActor?.GetEntity());
        }
    }
}
