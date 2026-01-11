using UnityEngine;
using Assets.Scripts.Effects.EffectTypes.Damage;

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
    public DamageFormula formula = new DamageFormula();

    /// <summary>
    /// Applies this damage effect to the target entity.
    /// Logging is handled automatically by DamageApplicator.
    /// 
    /// Parameter convention:
    /// - user: The Entity dealing damage (attacker)
    /// - target: The Entity receiving damage
    /// - context: WeaponComponent (for damage calculations) or null
    /// - source: Skill/EventCard that triggered this (for logging "Destroyed by X")
    /// </summary>
    public override void Apply(Entity user, Entity target, Object context = null, Object source = null)
    {
        // Extract weapon from context (if weapon-based skill)
        WeaponComponent weapon = context as WeaponComponent;
        
        // Calculate damage using appropriate method
        DamageBreakdown breakdown;
        if (weapon != null && formula.mode != SkillDamageMode.SkillOnly)
        {
            breakdown = formula.ComputeWithWeapon(weapon);
        }
        else
        {
            breakdown = formula.ComputeSkillOnly();
        }
        
        if (breakdown.rawTotal <= 0)
        {
            return;
        }
        
        // Determine source type
        DamageSource sourceType = weapon != null ? DamageSource.Weapon : DamageSource.Ability;
        
        // Apply damage - DamageApplicator handles logging automatically
        DamageApplicator.Apply(
            breakdown: breakdown,
            target: target,
            attacker: user,
            causalSource: source ?? weapon,
            sourceType: sourceType
        );
    }

    /// <summary>
    /// Get a description of this damage for UI/logging.
    /// </summary>
    public string GetDamageDescription(WeaponComponent weapon = null)
    {
        return formula.GetDescription(weapon);
    }
}
