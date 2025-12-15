using UnityEngine;

/// <summary>
/// Encapsulates damage calculation logic.
/// Describes HOW to compute damage based on weapon, skill dice, and mode.
/// Used by DamageEffect to keep damage calculation policy separate from application.
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
    /// Compute total damage based on mode and optional weapon.
    /// </summary>
    /// <param name="weapon">Optional weapon component (can be null for spells)</param>
    /// <param name="damageType">Output: the final damage type</param>
    /// <returns>Total damage rolled</returns>
    public int ComputeDamage(WeaponComponent weapon, out DamageType damageType)
    {
        int total = 0;
        damageType = skillDamageType;

        switch (mode)
        {
            case SkillDamageMode.SkillOnly:
                // Pure skill damage, ignore weapon entirely
                total = RollSkillDamage();
                damageType = skillDamageType;
                break;

            case SkillDamageMode.WeaponOnly:
                // Just weapon dice, no skill contribution
                if (weapon != null)
                {
                    total = weapon.RollDamage();
                    damageType = weapon.damageType;
                }
                else
                {
                    Debug.LogWarning("[DamageFormula] WeaponOnly mode but no weapon provided!");
                    total = 0;
                }
                break;

            case SkillDamageMode.WeaponPlusSkill:
                // Weapon dice + skill dice combined
                if (weapon != null)
                {
                    int weaponDmg = weapon.RollDamage();
                    int skillDmg = RollSkillDamage();
                    total = weaponDmg + skillDmg;
                    damageType = useWeaponDamageType ? weapon.damageType : skillDamageType;
                }
                else
                {
                    // Fallback to skill-only if no weapon
                    total = RollSkillDamage();
                    damageType = skillDamageType;
                }
                break;

            case SkillDamageMode.WeaponMultiplied:
                // Weapon dice multiplied (e.g., crits, sneak attack)
                if (weapon != null)
                {
                    int multipliedDice = Mathf.RoundToInt(weapon.damageDice * weaponMultiplier);
                    total = RollUtility.RollDamage(multipliedDice, weapon.damageDieSize, weapon.damageBonus);
                    damageType = weapon.damageType;
                }
                else
                {
                    Debug.LogWarning("[DamageFormula] WeaponMultiplied mode but no weapon provided!");
                    total = 0;
                }
                break;
        }

        return total;
    }

    /// <summary>
    /// Roll the skill's own dice.
    /// </summary>
    private int RollSkillDamage()
    {
        if (skillDice <= 0 || skillDieSize <= 0)
            return skillBonus; // Just flat bonus if no dice
            
        return RollUtility.RollDamage(skillDice, skillDieSize, skillBonus);
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
