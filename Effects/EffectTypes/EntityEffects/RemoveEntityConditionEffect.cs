using Assets.Scripts.Conditions;
using Assets.Scripts.Conditions.EntityConditions;
using Assets.Scripts.Entities;
using Assets.Scripts.Entities.Vehicles;
using SerializeReferenceEditor;
using UnityEngine;

namespace Assets.Scripts.Effects.EffectTypes.EntityEffects
{
    /// <summary>
    /// Removes status effects from a target by category or specific template.
    /// Dual-interface: use in EntityEffectInvocation to remove from a specific component,
    /// or in VehicleEffectInvocation to remove from all components on a vehicle.
    /// </summary>
    [System.Serializable]
    [SRName("Remove Condition")]
    public class RemoveEntityConditionEffect : IEntityEffect, IVehicleEffect
    {
        [Header("Removal Filter")]
        [Tooltip("Categories to remove (e.g., DoT removes all burning/bleeding). Leave None to use specific template.")]
        public ConditionCategory categoriesToRemove = ConditionCategory.None;

        [Tooltip("Optional: remove only this specific status effect template. Takes priority over categories.")]
        public EntityCondition specificTemplate;

        void IEntityEffect.Apply(Entity target, EffectContext context)
            => RemoveConditions(target.RemoveConditionsByTemplate, target.RemoveConditionsByCategory);

        void IVehicleEffect.Apply(Vehicle target, EffectContext context)
            => RemoveConditions(target.RemoveConditionsByTemplate, target.RemoveConditionsByCategory);

        private void RemoveConditions(
            System.Action<EntityCondition> removeByTemplate,
            System.Action<ConditionCategory> removeByCategory)
        {
            if (specificTemplate != null)
                removeByTemplate(specificTemplate);
            else if (categoriesToRemove != ConditionCategory.None)
                removeByCategory(categoriesToRemove);
            else
                Debug.LogWarning("[RemoveEntityConditionEffect] Neither specificTemplate nor categoriesToRemove set!");
        }
    }
}
