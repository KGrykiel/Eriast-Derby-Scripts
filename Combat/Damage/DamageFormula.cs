using UnityEngine;
using Assets.Scripts.Combat.Damage;

/// <summary>
/// Encapsulates damage calculation logic.
/// Describes HOW to compute damage based on dice configuration.
/// Used by DamageEffect to keep damage calculation policy separate from application.
/// 
/// NOTE: All dice rolling is delegated to RollUtility for consistency.
/// Weapon integration is handled through DamageMode - weapons are OPTIONAL.
/// </summary>
[System.Serializable]
public class DamageFormula
{
    [Header("Base Damage")]
    [Tooltip("Number of dice to roll")]
    public int baseDice = 1;
    
    [Tooltip("Size of each die (d4, d6, d8, d10, d12)")]
    public int dieSize = 6;
    
    [Tooltip("Flat bonus added to damage")]
    public int bonus = 0;

    [Header("Damage Type")]
    [Tooltip("Type of damage dealt")]
    public DamageType damageType = DamageType.Physical;

    [Header("Weapon Integration (Optional)")]
    [Tooltip("How this damage interacts with weapons")]
    public DamageMode mode = DamageMode.BaseOnly;
    
    [Tooltip("Override damage type with weapon's type when mode uses weapon")]
    public bool useWeaponDamageType = true;

    /// <summary>
    /// Compute damage based on the formula's mode.
    /// Handles all damage modes: base-only, weapon-only, weapon+base.
    /// Critical hits double ALL dice (weapon + base), not flat bonuses.
    /// </summary>
    /// <param name="weapon">Weapon component (required for weapon-based modes, ignored for BaseOnly)</param>
    /// <param name="isCriticalHit">If true, doubles all dice (weapon + base), not flat bonuses</param>
    public DamageResult Compute(WeaponComponent weapon = null, bool isCriticalHit = false)
    {
        var result = DamageResult.Create(damageType);

        switch (mode)
        {
            case DamageMode.BaseOnly:
                // Base dice only, weapon ignored (double dice on crit)
                if (baseDice > 0 && dieSize > 0)
                {
                    var (diceCount, label) = ApplyCritMultiplier(baseDice, "Base", isCriticalHit);
                    int rolled = RollUtility.RollDice(diceCount, dieSize);
                    DamageCalculator.AddSource(result, label, diceCount, dieSize, bonus, rolled, "Damage Effect");
                }
                else if (bonus != 0)
                {
                    DamageCalculator.AddFlat(result, "Bonus", bonus, "Damage Effect");
                }
                break;

            case DamageMode.WeaponOnly:
                if (weapon == null)
                {
                    Debug.LogWarning("[DamageFormula] WeaponOnly mode requires a weapon!");
                    return result; // Empty result
                }
                
                // Get weapon stats using accessor methods
                int weaponDamageDice = weapon.GetDamageDice();
                int weaponDieSize = weapon.GetDamageDieSize();
                int weaponDamageBonus = weapon.GetDamageBonus();
                
                // Weapon dice (double on crit, flat bonus never doubled)
                var (weaponDice, weaponLabel) = ApplyCritMultiplier(weaponDamageDice, "Weapon", isCriticalHit);
                int weaponRolled = RollUtility.RollDice(weaponDice, weaponDieSize);
                DamageCalculator.AddSource(result, weaponLabel, weaponDice, weaponDieSize, weaponDamageBonus, weaponRolled, weapon.name);
                result.damageType = weapon.damageType;
                break;

            case DamageMode.WeaponPlusBase:
                if (weapon == null)
                {
                    Debug.LogWarning("[DamageFormula] WeaponPlusBase mode requires a weapon!");
                    return result; // Empty result
                }
                
                // Get weapon stats using accessor methods
                int weaponDamageDice2 = weapon.GetDamageDice();
                int weaponDieSize2 = weapon.GetDamageDieSize();
                int weaponDamageBonus2 = weapon.GetDamageBonus();
                
                // Weapon dice (double on crit, flat bonus never doubled)
                var (weaponDice2, weaponLabel2) = ApplyCritMultiplier(weaponDamageDice2, "Weapon", isCriticalHit);
                int weaponRolled2 = RollUtility.RollDice(weaponDice2, weaponDieSize2);
                DamageCalculator.AddSource(result, weaponLabel2, weaponDice2, weaponDieSize2, weaponDamageBonus2, weaponRolled2, weapon.name);
                
                // Base dice (also doubled on crit!)
                if (baseDice > 0 && dieSize > 0)
                {
                    var (baseDiceCount, baseLabel) = ApplyCritMultiplier(baseDice, "Bonus", isCriticalHit);
                    int baseRolled = RollUtility.RollDice(baseDiceCount, dieSize);
                    DamageCalculator.AddSource(result, baseLabel, baseDiceCount, dieSize, bonus, baseRolled, "Damage Effect");
                }
                else if (bonus != 0)
                {
                    DamageCalculator.AddFlat(result, "Bonus", bonus, "Damage Effect");
                }
                
                result.damageType = useWeaponDamageType ? weapon.damageType : damageType;
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
