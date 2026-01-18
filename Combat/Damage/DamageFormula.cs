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

    [Header("Damage Type")]
    [Tooltip("If true, use weapon's damage type when weapon is present")]
    public bool useWeaponDamageType = true;
    
    [Tooltip("Damage type to use when no weapon or useWeaponDamageType is false")]
    public DamageType skillDamageType = DamageType.Physical;

    /// <summary>
    /// Compute damage based on the formula's mode.
    /// Handles all damage modes: skill-only, weapon-only, weapon+skill.
    /// Critical hits double ALL dice (weapon + skill), not flat bonuses.
    /// </summary>
    /// <param name="weapon">Weapon component (required for weapon-based modes, ignored for SkillOnly)</param>
    /// <param name="isCriticalHit">If true, doubles all dice (weapon + skill), not flat bonuses</param>
    public DamageResult Compute(WeaponComponent weapon = null, bool isCriticalHit = false)
    {
        var result = DamageResult.Create(skillDamageType);

        switch (mode)
        {
            case SkillDamageMode.SkillOnly:
                // Skill dice only, weapon ignored (double dice on crit)
                if (skillDice > 0 && skillDieSize > 0)
                {
                    var (diceCount, label) = ApplyCritMultiplier(skillDice, "Skill", isCriticalHit);
                    int rolled = RollUtility.RollDice(diceCount, skillDieSize);
                    DamageCalculator.AddSource(result, label, diceCount, skillDieSize, skillBonus, rolled, "Skill Effect");
                }
                else if (skillBonus != 0)
                {
                    DamageCalculator.AddFlat(result, "Skill Bonus", skillBonus, "Skill Effect");
                }
                break;

            case SkillDamageMode.WeaponOnly:
                if (weapon == null)
                {
                    Debug.LogWarning("[DamageFormula] WeaponOnly mode requires a weapon!");
                    return result; // Empty result
                }
                
                // Weapon dice (double on crit, flat bonus never doubled)
                var (weaponDice, weaponLabel) = ApplyCritMultiplier(weapon.damageDice, "Weapon", isCriticalHit);
                int weaponRolled = RollUtility.RollDice(weaponDice, weapon.damageDieSize);
                DamageCalculator.AddSource(result, weaponLabel, weaponDice, weapon.damageDieSize, weapon.damageBonus, weaponRolled, weapon.name);
                result.damageType = weapon.damageType;
                break;

            case SkillDamageMode.WeaponPlusSkill:
                if (weapon == null)
                {
                    Debug.LogWarning("[DamageFormula] WeaponPlusSkill mode requires a weapon!");
                    return result; // Empty result
                }
                
                // Weapon dice (double on crit, flat bonus never doubled)
                var (weaponDice2, weaponLabel2) = ApplyCritMultiplier(weapon.damageDice, "Weapon", isCriticalHit);
                int weaponRolled2 = RollUtility.RollDice(weaponDice2, weapon.damageDieSize);
                DamageCalculator.AddSource(result, weaponLabel2, weaponDice2, weapon.damageDieSize, weapon.damageBonus, weaponRolled2, weapon.name);
                
                // Skill dice (also doubled on crit!)
                if (skillDice > 0 && skillDieSize > 0)
                {
                    var (skillDiceCount, skillLabel) = ApplyCritMultiplier(skillDice, "Skill Bonus", isCriticalHit);
                    int skillRolled = RollUtility.RollDice(skillDiceCount, skillDieSize);
                    DamageCalculator.AddSource(result, skillLabel, skillDiceCount, skillDieSize, skillBonus, skillRolled, "Skill Effect");
                }
                else if (skillBonus != 0)
                {
                    DamageCalculator.AddFlat(result, "Skill Bonus", skillBonus, "Skill Effect");
                }
                
                result.damageType = useWeaponDamageType ? weapon.damageType : skillDamageType;
                break;
        }

        DamageCalculator.ApplyResistance(result, ResistanceLevel.Normal);
        return result;
    }
    
    /// <summary>
    /// Apply critical hit multiplier to dice count and update label.
    /// Doubles dice on crit, appends "(CRIT)" to label.
    /// </summary>
    private static (int diceCount, string label) ApplyCritMultiplier(int baseDice, string baseLabel, bool isCrit)
    {
        if (isCrit)
        {
            return (baseDice * 2, $"{baseLabel} (CRIT)");
        }
        return (baseDice, baseLabel);
    }
}
