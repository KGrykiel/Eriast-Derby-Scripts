using UnityEngine;
using RacingGame.Events;

/// <summary>
/// Universal damage effect.
/// Uses DamageFormula to calculate damage based on mode (skill-only, weapon-based, etc.)
/// Can work with or without a weapon depending on formula configuration.
/// 
/// Damage Flow:
/// 1. DamageEffect.Apply() is called by SkillEffectApplicator
/// 2. DamageFormula.ComputeDamageWithBreakdown() calculates damage (uses RollUtility)
/// 3. DamagePacket is created with raw damage
/// 4. DamageResolver applies resistances and returns final damage
/// 5. Entity.TakeDamage() reduces HP
/// </summary>
[System.Serializable]
public class DamageEffect : EffectBase
{
    [Header("Damage Formula")]
    [Tooltip("Defines how damage is calculated (skill dice, weapon scaling, etc.)")]
    public DamageFormula formula = new DamageFormula();

    // Store last breakdown for retrieval by SkillEffectApplicator
    private DamageBreakdown lastBreakdown;
    
    /// <summary>
    /// Gets the full breakdown of the last damage calculation.
    /// Used by SkillEffectApplicator for logging.
    /// </summary>
    public DamageBreakdown LastBreakdown => lastBreakdown;

    /// <summary>
    /// Applies this damage effect to the target entity.
    /// 
    /// Parameter convention from Skill.Use():
    /// - context: WeaponComponent (for damage calculations) or null
    /// - source: Skill that triggered this effect (for modifier tracking)
    /// 
    /// Tries to extract weapon from both context and source for backward compatibility.
    /// </summary>
    public override void Apply(Entity user, Entity target, Object context = null, Object source = null)
    {
        // Try to extract weapon from context first (new convention), then source (old convention)
        WeaponComponent weapon = context as WeaponComponent ?? source as WeaponComponent;
        
        // Calculate damage using the formula with full breakdown
        lastBreakdown = formula.ComputeDamageWithBreakdown(weapon);
        
        if (lastBreakdown.rawTotal <= 0)
        {
            return;
        }
        
        // Create damage packet
        DamagePacket packet = DamagePacket.Create(lastBreakdown.rawTotal, lastBreakdown.damageType, user);
        
        // If we have a weapon, mark it as weapon damage
        if (weapon != null)
        {
            packet.sourceType = DamageSource.Weapon;
        }
        
        // Resolve damage through the central resolver (handles resistances, etc.)
        int resolvedDamage = DamageResolver.ResolveDamage(packet, target);
        
        // Update breakdown with actual resistance info from target
        ResistanceLevel resistance = target.GetResistance(lastBreakdown.damageType);
        lastBreakdown.WithResistance(resistance);
        lastBreakdown.finalDamage = resolvedDamage;
        
        // Apply the resolved damage to target
        target.TakeDamage(resolvedDamage);
    }

    /// <summary>
    /// Get a description of this damage for UI/logging.
    /// </summary>
    public string GetDamageDescription(WeaponComponent weapon = null)
    {
        return formula.GetDescription(weapon);
    }
    
    /// <summary>
    /// Get detailed breakdown string for tooltips.
    /// </summary>
    public string GetDetailedBreakdown()
    {
        return lastBreakdown?.ToDetailedString() ?? "No damage calculated yet";
    }
}
