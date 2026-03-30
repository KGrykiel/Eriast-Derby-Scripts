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

            Entity entity = ResolveEntity(target);
            if (entity == null) return;

            entity.ApplyCondition(condition, context.SourceActor?.GetEntity());
        }

        protected override Entity ResolveEntity(IEffectTarget target)
        {
            switch (target)
            {
                case Entity e:
                    return e;
                case Vehicle vehicle:
                    return ResolveVehicleTarget(vehicle);
                case VehicleSeat:
                    Debug.LogWarning($"[{GetType().Name}] VehicleSeat is not a valid target for this effect.");
                    return null;
                default:
                    Debug.LogWarning($"[{GetType().Name}] Unsupported target type: {(target != null ? target.GetType().Name : "null")}");
                    return null;
            }
        }

        private Entity ResolveVehicleTarget(Vehicle vehicle)
        {
            // Route based on the first modifier's attribute so the condition lands on the correct component.
            // TODO: for conditions with multiple modifiers targeting different components,
            // each modifier should ideally be applied to its own component.
            if (condition.modifiers != null && condition.modifiers.Count > 0)
            {
                var firstModifier = condition.modifiers[0];
                var component = VehicleComponentResolver.ResolveForAttribute(vehicle, firstModifier.attribute);
                if (component != null) return component;
            }

            return vehicle.chassis;
        }
    }
}

