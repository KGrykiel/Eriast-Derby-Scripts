using UnityEngine;
using Combat.Damage;

/// <summary>
/// Encapsulates damage calculation logic.
/// Describes HOW to compute damage based on dice configuration.
/// Used by DamageEffect to keep damage calculation policy separate from application.
/// 
/// NOTE: All dice rolling is delegated to RollUtility for consistency.
/// Weapon integration is handled through SkillDamageMode - weapons are OPTIONAL.
/// </summary>
[System.Serializable]
public class DamageFormula
{
    [Header("Damage Mode")]
    [Tooltip("How this damage interacts with weapons")]
    public SkillDamageMode mode = SkillDamageMode.SkillOnly;

    [Header("Skill Dice (used for SkillOnly and WeaponPlusSkill modes)")]
    [Tooltip("Number of skill bonus dice")]
    public int skillDice = 1;
    
    [Tooltip("Size of skill dice (4, 6, 8, 10, 12)")]
    public int skillDieSize = 6;
    
    [Tooltip("Flat bonus added to skill damage")]
    public int skillBonus = 0;

    [Header("Weapon Multiplier (for WeaponMultiplied mode)")]
    [Tooltip("Multiplier for weapon dice count (e.g., 2.0 for double dice on crit)")]
    public float weaponMultiplier = 2.0f;

    [Header("Damage Type")]
    [Tooltip("If true, use weapon's damage type when weapon is present")]
    public bool useWeaponDamageType = true;
    
    [Tooltip("Damage type to use when no weapon or useWeaponDamageType is false")]
    public DamageType skillDamageType = DamageType.Physical;

    /// <summary>
    /// Compute damage for a skill-only formula (no weapon).
    /// Used for spells, abilities, and other non-weapon skills.
    /// </summary>
    public DamageResult ComputeSkillOnly()
    {
        if (mode != SkillDamageMode.SkillOnly)
        {
            Debug.LogWarning($"[DamageFormula] ComputeSkillOnly() called but mode is {mode}. Use ComputeWithWeapon() instead.");
        }
        
        int rolled = RollUtility.RollDice(skillDice, skillDieSize);
        
        var result = DamageResult.Create(skillDamageType);
        DamageCalculator.AddSource(result, "Skill", skillDice, skillDieSize, skillBonus, rolled, "Skill Effect");
        DamageCalculator.ApplyResistance(result, ResistanceLevel.Normal);
        
        return result;
    }

    /// <summary>
    /// Compute damage with weapon integration.
    /// Used for weapon attacks and weapon-enhanced skills.
    /// </summary>
    public DamageResult ComputeWithWeapon(WeaponComponent weapon)
    {
        if (weapon == null)
        {
            Debug.LogWarning($"[DamageFormula] ComputeWithWeapon() called with null weapon. Falling back to skill-only.");
            return ComputeSkillOnly();
        }
        
        var result = DamageResult.Create(skillDamageType);

        switch (mode)
        {
            case SkillDamageMode.SkillOnly:
                // Should use ComputeSkillOnly() instead, but handle gracefully
                Debug.LogWarning("[DamageFormula] SkillOnly mode called with weapon. Use ComputeSkillOnly() instead.");
                return ComputeSkillOnly();

            case SkillDamageMode.WeaponOnly:
                // Just weapon dice, no skill contribution
                int weaponRolled = RollUtility.RollDice(weapon.damageDice, weapon.damageDieSize);
                DamageCalculator.AddSource(result, "Weapon", weapon.damageDice, weapon.damageDieSize, weapon.damageBonus, weaponRolled, weapon.name);
                result.damageType = weapon.damageType;
                break;

            case SkillDamageMode.WeaponPlusSkill:
                // Weapon dice + skill dice combined
                int weaponRolled2 = RollUtility.RollDice(weapon.damageDice, weapon.damageDieSize);
                DamageCalculator.AddSource(result, "Weapon", weapon.damageDice, weapon.damageDieSize, weapon.damageBonus, weaponRolled2, weapon.name);
                
                if (skillDice > 0 && skillDieSize > 0)
                {
                    int skillRolled = RollUtility.RollDice(skillDice, skillDieSize);
                    DamageCalculator.AddSource(result, "Skill Bonus", skillDice, skillDieSize, skillBonus, skillRolled, "Skill Effect");
                }
                else if (skillBonus != 0)
                {
                    DamageCalculator.AddFlat(result, "Skill Bonus", skillBonus, "Skill Effect");
                }
                
                result.damageType = useWeaponDamageType ? weapon.damageType : skillDamageType;
                break;

            case SkillDamageMode.WeaponMultiplied:
                // Weapon dice multiplied (e.g., crits, sneak attack)
                int multipliedDice = Mathf.RoundToInt(weapon.damageDice * weaponMultiplier);
                int multipliedRolled = RollUtility.RollDice(multipliedDice, weapon.damageDieSize);
                DamageCalculator.AddSource(result, $"Weapon ×{weaponMultiplier}", multipliedDice, weapon.damageDieSize, weapon.damageBonus, multipliedRolled, weapon.name);
                result.damageType = weapon.damageType;
                break;
        }

        DamageCalculator.ApplyResistance(result, ResistanceLevel.Normal);
        return result;
    }

    /// <summary>
    /// Get a description of this formula for UI/tooltips.
    /// </summary>
    public string GetDescription(WeaponComponent weapon = null)
    {
        switch (mode)
        {
            case SkillDamageMode.SkillOnly:
                return FormatDice(skillDice, skillDieSize, skillBonus, skillDamageType);

            case SkillDamageMode.WeaponOnly:
                if (weapon != null)
                    return weapon.DamageString;
                return "Weapon damage";

            case SkillDamageMode.WeaponPlusSkill:
                string skillPart = FormatDice(skillDice, skillDieSize, skillBonus, null);
                if (weapon != null)
                {
                    return $"{weapon.DamageString} + {skillPart}";
                }
                return $"Weapon + {skillPart}";

            case SkillDamageMode.WeaponMultiplied:
                if (weapon != null)
                {
                    int dice = Mathf.RoundToInt(weapon.damageDice * weaponMultiplier);
                    return FormatDice(dice, weapon.damageDieSize, weapon.damageBonus, weapon.damageType);
                }
                return $"Weapon ×{weaponMultiplier}";

            default:
                return "Unknown";
        }
    }

    private string FormatDice(int dice, int size, int bonus, DamageType? type)
    {
        string result = $"{dice}d{size}";
        if (bonus > 0) result += $"+{bonus}";
        else if (bonus < 0) result += $"{bonus}";
        if (type.HasValue) result += $" {type.Value}";
        return result;
    }
}
