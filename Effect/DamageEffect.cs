using UnityEngine;
using RacingGame.Events;

/// <summary>
/// Universal damage effect.
/// Uses DamageFormula to calculate damage based on mode (skill-only, weapon-based, etc.)
/// Can work with or without a weapon depending on formula configuration.
/// </summary>
[System.Serializable]
public class DamageEffect : EffectBase
{
    [Header("Damage Formula")]
    [Tooltip("Defines how damage is calculated (skill dice, weapon scaling, etc.)")]
    public DamageFormula formula = new DamageFormula();

    // Store last roll breakdown for retrieval by Skill.Use()
    private DamageBreakdown lastBreakdown;

    /// <summary>
    /// Gets the last damage rolled by this effect.
    /// </summary>
    public int LastDamageRolled => lastBreakdown?.finalDamage ?? 0;
    
    /// <summary>
    /// Gets the damage type used in the last application.
    /// </summary>
    public DamageType LastDamageType => lastBreakdown?.damageType ?? DamageType.Physical;
    
    /// <summary>
    /// Gets the full breakdown of the last damage calculation.
    /// </summary>
    public DamageBreakdown LastBreakdown => lastBreakdown;

    /// <summary>
    /// Applies this damage effect to the target entity.
    /// Extracts weapon from source (if provided) and uses formula to calculate damage.
    /// </summary>
    public override void Apply(Entity user, Entity target, Object context = null, Object source = null)
    {
        // Try to extract weapon from source (optional)
        WeaponComponent weapon = source as WeaponComponent;
        
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
