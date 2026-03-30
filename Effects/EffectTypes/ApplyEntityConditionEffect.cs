using System.Collections.Generic;
using Assets.Scripts.Conditions.EntityConditions;
using Assets.Scripts.Entities;
using Assets.Scripts.Entities.Vehicles;
using SerializeReferenceEditor;
using UnityEngine;

/// <summary>
/// Applies an entity condition (buff, debuff, DoT, HoT, etc.) directly to the targeted entity.
/// For applying a condition to the character operating a component, use ApplyCharacterConditionEffect instead.
/// </summary>
namespace Assets.Scripts.Effects.EffectTypes
{
    [System.Serializable]
    [SRName("Apply Entity Condition")]
    public class ApplyEntityConditionEffect : EffectBase
    {
        [Header("Entity Condition")]
        [Tooltip("The EntityCondition asset to apply (create via Racing/Entity Condition menu)")]
        public EntityCondition condition;

        public override void Apply(IEffectTarget target, EffectContext context)
        {
            if (condition == null)
            {
                Debug.LogWarning("[ApplyEntityConditionEffect] No condition assigned!");
                return;
            }

            if (target is Vehicle vehicle)
            {
                ApplyToVehicle(vehicle, context.SourceActor?.GetEntity());
                return;
            }

            Entity entity = ResolveEntity(target);
            if (entity == null) return;

            entity.ApplyCondition(condition, context.SourceActor?.GetEntity());
        }

        private void ApplyToVehicle(Vehicle vehicle, Object applier)
        {
            if (condition.modifiers == null || condition.modifiers.Count == 0)
            {
                vehicle.chassis.ApplyCondition(condition, applier);
                return;
            }

            // Apply to each unique component that owns at least one of this condition's modifiers.
            // A condition with [EnergyRegen, MaxSpeed] lands on PowerCore AND Drive, not just the first.
            var targets = new HashSet<Entity>();
            foreach (var modifier in condition.modifiers)
            {
                Entity component = VehicleComponentResolver.ResolveForAttribute(vehicle, modifier.attribute);
                targets.Add(component != null ? component : vehicle.chassis);
            }

            foreach (var entity in targets)
                entity.ApplyCondition(condition, applier);
        }
    }
}

