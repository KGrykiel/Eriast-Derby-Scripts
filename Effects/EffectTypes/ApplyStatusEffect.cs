using UnityEngine;
using StatusEffectTemplate = Assets.Scripts.StatusEffects.StatusEffect;
using Assets.Scripts.Effects;

/// <summary>
/// All kinds of status effects. Used in conjunction with the status effect system 
/// (StatusEffectTemplate, StatusEffectInstance, and Entity.ApplyStatusEffect) to apply buffs, debuffs, DoTs, HoTs, etc. to entities.
/// </summary>
[System.Serializable]
public class ApplyStatusEffect : EffectBase
{
    [Header("Status Effect")]
    [Tooltip("The StatusEffect asset to apply (create via Racing/Status Effect menu)")]
    public StatusEffectTemplate statusEffect;

    public override void Apply(Entity user, Entity target, EffectContext context, Object source = null)
    {
        if (statusEffect == null)
        {
            Debug.LogWarning("[ApplyStatusEffect] No status effect assigned!");
            return;
        }

        target.ApplyStatusEffect(statusEffect, source != null ? source : user);
    }
}

