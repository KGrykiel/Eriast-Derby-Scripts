using Assets.Scripts.Conditions;
using Assets.Scripts.Conditions.VehicleConditions;
using Assets.Scripts.Entities.Vehicles;
using SerializeReferenceEditor;
using UnityEngine;

namespace Assets.Scripts.Effects.EffectTypes.VehicleEffects
{
    /// <summary>
    /// Removes vehicle-wide conditions by category or specific template.
    /// Only affects conditions tracked by the vehicle's VehicleConditionManager.
    /// To remove entity conditions from all components use RemoveEntityConditionEffect in a VehicleEffectInvocation.
    /// </summary>
    [System.Serializable]
    [SRName("Remove Vehicle Condition")]
    public class RemoveVehicleConditionEffect : IVehicleEffect
    {
        [Header("Removal Filter")]
        [Tooltip("Categories to remove. Leave None to use specific template.")]
        public ConditionCategory categoriesToRemove = ConditionCategory.None;

        [Tooltip("Optional: remove only this specific condition template. Takes priority over categories.")]
        public VehicleCondition specificTemplate;

        void IVehicleEffect.Apply(Vehicle target, EffectContext context)
        {
            if (specificTemplate != null)
                target.RemoveVehicleConditionsByTemplate(specificTemplate);
            else if (categoriesToRemove != ConditionCategory.None)
                target.RemoveVehicleConditionsByCategory(categoriesToRemove);
            else
                Debug.LogWarning("[RemoveVehicleConditionEffect] Neither specificTemplate nor categoriesToRemove set!");
        }
    }
}
