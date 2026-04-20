using System;
using Assets.Scripts.Conditions;
using Assets.Scripts.Conditions.VehicleConditions;
using Assets.Scripts.Entities.Vehicles;
using SerializeReferenceEditor;
using UnityEngine;

namespace Assets.Scripts.Effects.EffectTypes.VehicleEffects
{
    /// <summary>
    /// Removes vehicle-wide conditions by category.
    /// Only affects conditions tracked by the vehicle's VehicleConditionManager.
    /// </summary>
    [Serializable]
    [SRName("Remove Vehicle Condition/By Category")]
    public class RemoveVehicleConditionByCategoryEffect : IVehicleEffect
    {
        [Tooltip("Categories to remove.")]
        public ConditionCategory categoriesToRemove = ConditionCategory.None;

        void IVehicleEffect.Apply(Vehicle target, EffectContext context)
        {
            if (categoriesToRemove == ConditionCategory.None)
            {
                Debug.LogWarning($"[RemoveVehicleConditionByCategoryEffect] No categories set — effect had no impact. Causal source: {context.CausalSource ?? "unknown"}");
                return;
            }

            target.RemoveVehicleConditionsByCategory(categoriesToRemove);
        }
    }

    /// <summary>
    /// Removes a specific vehicle-wide condition template.
    /// Only affects conditions tracked by the vehicle's VehicleConditionManager.
    /// </summary>
    [Serializable]
    [SRName("Remove Vehicle Condition/By Template")]
    public class RemoveVehicleConditionByTemplateEffect : IVehicleEffect
    {
        [Tooltip("Specific condition template to remove.")]
        public VehicleCondition template;

        void IVehicleEffect.Apply(Vehicle target, EffectContext context)
        {
            if (template == null)
            {
                Debug.LogWarning($"[RemoveVehicleConditionByTemplateEffect] No template assigned — effect had no impact. Causal source: {context.CausalSource ?? "unknown"}");
                return;
            }

            target.RemoveVehicleConditionsByTemplate(template);
        }
    }
}
