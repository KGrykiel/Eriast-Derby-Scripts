using UnityEngine;
using Assets.Scripts.Combat.Damage;
using Assets.Scripts.Effects;

/// <summary>
/// Damage effect for skills and event cards.
/// Uses DamageFormula to calculate damage, then DamageApplicator to apply it.
/// 
/// This effect is STATELESS - DamageApplicator handles logging automatically.
/// 
/// Damage Flow:
/// 1. DamageEffect.Apply() is called by SkillEffectApplicator
/// 2. DamageFormula computes damage (ComputeSkillOnly or ComputeWithWeapon)
/// 3. DamageApplicator.Apply() applies damage AND logs it
/// </summary>
[System.Serializable]
public class DamageEffect : EffectBase
{
    [Header("Damage Formula")]
    [Tooltip("Defines how damage is calculated (skill dice, weapon scaling, etc.)")]
    public DamageFormula formula = new();

    /// <summary>
    /// Applies this damage effect to the target entity.
    /// Logging is handled automatically by DamageApplicator.
    /// 
    /// Parameter convention:
    /// - user: The Entity dealing damage (attacker) - may be a WeaponComponent
    /// - target: The Entity receiving damage
    /// - context: EffectContext with situational data (crits, etc.)
    /// - source: Skill/EventCard that triggered this (for logging "Destroyed by X")
    /// </summary>
    public override void Apply(Entity user, Entity target, EffectContext context, Object source = null)
    {
        // Extract weapon from user (if it's a weapon component)
        WeaponComponent weapon = user as WeaponComponent;
        
        // Calculate damage - formula handles all modes automatically
        DamageResult result = formula.Compute(weapon, context.IsCriticalHit);
        
        if (result.RawTotal <= 0)
        {
            return;
        }
        
        // Determine source type
        DamageSource sourceType = weapon != null ? DamageSource.Weapon : DamageSource.Ability;
        
        // Apply damage - DamageApplicator handles logging automatically
        DamageApplicator.Apply(
            result: result,
            target: target,
            attacker: user,
            causalSource: source != null ? source : weapon,
            sourceType: sourceType
        );
    }
}
