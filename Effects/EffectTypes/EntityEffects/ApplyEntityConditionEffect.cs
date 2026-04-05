using Assets.Scripts.Conditions.EntityConditions;
using Assets.Scripts.Entities;
using Assets.Scripts.Entities.Vehicles;
using SerializeReferenceEditor;
using UnityEngine;

/// <summary>
/// Applies an entity condition (buff, debuff, DoT, HoT, etc.) to a target.
/// Dual-interface: use in EntityEffectInvocation to apply directly to a specific component,
/// or in VehicleEffectInvocation to let the vehicle route based on the condition's modifier attributes.
/// </summary>
namespace Assets.Scripts.Effects.EffectTypes.EntityEffects
{
    [System.Serializable]
    [SRName("Apply Entity Condition")]
    public class ApplyEntityConditionEffect : IEntityEffect, IVehicleEffect
    {
        [Header("Entity Condition")]
        [Tooltip("The EntityCondition asset to apply (create via Racing/Entity Condition menu)")]
        public EntityCondition condition;

        void IEntityEffect.Apply(Entity target, EffectContext context)
            => target.ApplyCondition(condition, context.SourceActor?.GetEntity());

        void IVehicleEffect.Apply(Vehicle target, EffectContext context)
            => target.ApplyCondition(condition, context.SourceActor?.GetEntity());
    }
}

