using UnityEngine;
using Assets.Scripts.Conditions.EntityConditions;
using Assets.Scripts.Effects;

/// <summary>
/// All kinds of status effects. Used in conjunction with the status effect system 
/// (StatusEffectTemplate, StatusEffectInstance, and Entity.ApplyStatusEffect) to apply buffs, debuffs, DoTs, HoTs, etc. to entities.
/// </summary>
[System.Serializable]
public class ApplyConditionEffect : EffectBase
{
    [Header("Status Effect")]
    [Tooltip("The StatusEffect asset to apply (create via Racing/Status Effect menu)")]
    public EntityCondition condition;

    public override void Apply(Entity target, EffectContext context)
    {
        if (condition == null)
        {
            Debug.LogWarning("[ApplyStatusEffect] No status effect assigned!");
            return;
        }

        target.ApplyStatusEffect(condition, context.SourceEntity);
    }
}

