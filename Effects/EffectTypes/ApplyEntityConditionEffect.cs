using Assets.Scripts.Conditions.EntityConditions;
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

        public override void Apply(Entity target, EffectContext context)
        {
            if (condition == null)
            {
                Debug.LogWarning("[ApplyEntityConditionEffect] No condition assigned!");
                return;
            }

            target.ApplyCondition(condition, context.SourceActor?.GetEntity());
        }
    }
}

