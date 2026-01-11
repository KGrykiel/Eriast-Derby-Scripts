using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Assets.Scripts.Combat.Damage
{
    /// <summary>
    /// Handles all damage calculation logic.
    /// Pure logic - operates on DamageResult data.
    /// 
    /// Responsibilities:
    /// - Creating DamageResult from various sources (weapon, dice, flat)
    /// - Adding damage sources to results
    /// - Applying resistance and recalculating final damage
    /// - Resistance lookup from entities
    /// </summary>
    public static class DamageCalculator
    {
        // ==================== RESULT MODIFICATION ====================
        
        /// <summary>
        /// Add a damage source to a result and recalculate totals.
        /// </summary>
        public static void AddSource(DamageResult result, string name, int diceCount, int dieSize, int bonus, int rolled, string sourceName = null)
        {
            result.sources.Add(new DamageSourceEntry(name, diceCount, dieSize, bonus, rolled, sourceName));
            RecalculateFinal(result);
        }
        
        /// <summary>
        /// Add a flat damage amount (no dice) to a result.
        /// </summary>
        public static void AddFlat(DamageResult result, string name, int value, string sourceName = null)
        {
            if (value != 0)
            {
                result.sources.Add(new DamageSourceEntry(name, 0, 0, value, 0, sourceName));
                RecalculateFinal(result);
            }
        }
        
        // ==================== RESISTANCE ====================
        
        /// <summary>
        /// Apply resistance level and recalculate final damage.
        /// </summary>
        public static void ApplyResistance(DamageResult result, ResistanceLevel level)
        {
            result.resistanceLevel = level;
            RecalculateFinal(result);
        }
        
        /// <summary>
        /// Get an entity's resistance level to a damage type.
        /// </summary>
        public static ResistanceLevel GetResistance(Entity target, DamageType type)
        {
            if (target == null) return ResistanceLevel.Normal;
            return target.GetResistance(type);
        }
        
        /// <summary>
        /// Apply resistance multiplier to a raw damage amount.
        /// </summary>
        public static int ApplyResistanceMultiplier(int damage, ResistanceLevel resistance)
        {
            return resistance switch
            {
                ResistanceLevel.Vulnerable => damage * 2,
                ResistanceLevel.Resistant => damage / 2,
                ResistanceLevel.Immune => 0,
                _ => damage
            };
        }
        
        /// <summary>
        /// Recalculate final damage based on raw total and resistance.
        /// </summary>
        public static void RecalculateFinal(DamageResult result)
        {
            result.finalDamage = ApplyResistanceMultiplier(result.RawTotal, result.resistanceLevel);
        }
        
        // ==================== FACTORY METHODS ====================
        
        /// <summary>
        /// Create a damage result from a weapon component.
        /// </summary>
        public static DamageResult FromWeapon(WeaponComponent weapon)
        {
            int rolled = RollUtility.RollDice(weapon.damageDice, weapon.damageDieSize);
            
            var result = DamageResult.Create(weapon.damageType);
            AddSource(result, "Weapon", weapon.damageDice, weapon.damageDieSize, weapon.damageBonus, rolled, weapon.name);
            
            return result;
        }
        
        /// <summary>
        /// Create a damage result from a weapon with multiplied dice (for crits, sneak attack).
        /// </summary>
        public static DamageResult FromWeaponMultiplied(WeaponComponent weapon, float multiplier)
        {
            int multipliedDice = Mathf.RoundToInt(weapon.damageDice * multiplier);
            int rolled = RollUtility.RollDice(multipliedDice, weapon.damageDieSize);
            
            var result = DamageResult.Create(weapon.damageType);
            AddSource(result, $"Weapon ×{multiplier}", multipliedDice, weapon.damageDieSize, weapon.damageBonus, rolled, weapon.name);
            
            return result;
        }
        
        /// <summary>
        /// Create a flat damage result (for environmental/DoT).
        /// </summary>
        public static DamageResult FromFlat(int damage, DamageType damageType, string sourceName)
        {
            var result = DamageResult.Create(damageType);
            if (damage != 0)
            {
                AddFlat(result, sourceName, damage, sourceName);
            }
            return result;
        }
        
        /// <summary>
        /// Create a dice-based damage result (for skill damage, environmental effects).
        /// </summary>
        public static DamageResult FromDice(int diceCount, int dieSize, int bonus, DamageType damageType, string sourceName)
        {
            int rolled = RollUtility.RollDice(diceCount, dieSize);
            
            var result = DamageResult.Create(damageType);
            AddSource(result, sourceName, diceCount, dieSize, bonus, rolled, sourceName);
            
            return result;
        }
    }
}
