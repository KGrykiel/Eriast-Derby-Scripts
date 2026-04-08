using Assets.Scripts.Conditions.EntityConditions;
using Assets.Scripts.Entities;
using SerializeReferenceEditor;
using UnityEngine;

/// <summary>
/// Applies an entity condition (buff, debuff, DoT, HoT, etc.) to a specific component target.
/// Use in EntityEffectInvocation. For vehicle-wide conditions use ApplyVehicleConditionEffect instead.
/// </summary>
namespace Assets.Scripts.Effects.EffectTypes.EntityEffects
{
    [System.Serializable]
    [SRName("Apply Entity Condition")]
    public class ApplyEntityConditionEffect : IEntityEffect
    {
        [Header("Entity Condition")]
        [Tooltip("The EntityCondition asset to apply (create via Racing/Entity Condition menu)")]
        public EntityCondition condition;

        void IEntityEffect.Apply(Entity target, EffectContext context)
            => target.ApplyCondition(condition, context.SourceActor?.GetEntity());
    }
}

