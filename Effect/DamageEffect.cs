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

    // Store last rolled damage for retrieval by Skill.Use()
    private int lastDamageRolled = 0;
    private DamageType lastDamageType = DamageType.Physical;

    /// <summary>
    /// Gets the last damage rolled by this effect.
    /// Used by Skill.Use() to log accurate damage values.
    /// </summary>
    public int LastDamageRolled => lastDamageRolled;
    
    /// <summary>
    /// Gets the damage type used in the last application.
    /// </summary>
    public DamageType LastDamageType => lastDamageType;

    /// <summary>
    /// Applies this damage effect to the target entity.
    /// Extracts weapon from source (if provided) and uses formula to calculate damage.
    /// </summary>
    public override void Apply(Entity user, Entity target, Object context = null, Object source = null)
    {
        // Try to extract weapon from source (optional)
        WeaponComponent weapon = source as WeaponComponent;
        
        // Calculate damage using the formula
        DamageType damageType;
        int damage = formula.ComputeDamage(weapon, out damageType);
        
        if (damage <= 0)
        {
            lastDamageRolled = 0;
            lastDamageType = damageType;
            return;
        }
        
        // Create damage packet
        DamagePacket packet = DamagePacket.Create(damage, damageType, user);
        
        // If we have a weapon, mark it as weapon damage
        if (weapon != null)
        {
            packet.sourceType = DamageSource.Weapon;
        }
        
        // Resolve damage through the central resolver (handles resistances, etc.)
        lastDamageRolled = DamageResolver.ResolveDamage(packet, target);
        lastDamageType = damageType;
        
        // Apply the resolved damage to target
        target.TakeDamage(lastDamageRolled);
    }

    /// <summary>
    /// Get a description of this damage for UI/logging.
    /// </summary>
    public string GetDamageDescription(WeaponComponent weapon = null)
    {
        return formula.GetDescription(weapon);
    }
}
