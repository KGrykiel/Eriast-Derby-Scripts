using System.Collections.Generic;
using UnityEngine;
using Core;

namespace Combat.Attacks
{
    /// <summary>
    /// Calculator for attack rolls (d20 + modifiers vs AC).
    /// 
    /// Responsibilities:
    /// - Rolling d20 attacks
    /// - Gathering attack modifiers from all sources
    /// - Evaluating hit/miss against AC
    /// - Crit/fumble detection
    /// 
    /// DESIGN: Entities store raw base values. This calculator gathers modifiers
    /// from all sources and computes final values. Provides breakdown data for tooltips.
    /// 
    /// NOTE: Defense values (AC) delegated to StatCalculator (single source of truth).
    /// </summary>
    public static class AttackCalculator
    {
        // ==================== ATTACK ROLLING ====================
        
        /// <summary>
        /// Perform a complete attack roll with modifiers and evaluation.
        /// This is the primary method for making attacks.
        /// Handles critical hits (natural 20) and critical misses (natural 1).
        /// </summary>
        public static AttackResult PerformAttack(
            Entity attacker,
            Entity target,
            VehicleComponent sourceComponent = null,
            Skill skill = null,
            int additionalPenalty = 0)
        {
            // Roll the d20
            var result = RollAttack();
            
            // Gather and add attack modifiers
            var modifiers = GatherAttackModifiers(attacker, sourceComponent, skill);
            D20RollHelpers.AddModifiers(result, modifiers);
            
            // Add any additional penalty (e.g., component targeting)
            if (additionalPenalty != 0)
            {
                AddPenalty(result, additionalPenalty, "Targeting Penalty");
            }
            
            // Get target's defense value
            int defenseValue = StatCalculator.GatherDefenseValue(target);
            result.targetValue = defenseValue;
            
            // Evaluate: Critical hit (natural 20) auto-hits, critical miss (natural 1) auto-misses
            if (IsNatural20(result))
            {
                result.success = true;
                result.isCriticalHit = true;
            }
            else if (IsNatural1(result))
            {
                result.success = false;
                result.isCriticalMiss = true;
            }
            else
            {
                // Normal evaluation
                D20RollHelpers.EvaluateAgainstTarget(result, defenseValue);
            }
            
            return result;
        }
        
        /// <summary>
        /// Roll a d20 attack and create an attack result.
        /// </summary>
        public static AttackResult RollAttack()
        {
            int roll = RollUtility.RollD20();
            return AttackResult.FromD20(roll);
        }
        
        // ==================== ATTACK MODIFIER GATHERING ====================
        
        /// <summary>
        /// Gather attack modifiers from all sources.
        /// 
        /// Sources:
        /// - Intrinsic (weapon, character): Raw values converted to modifiers here
        /// - Applied (buffs, debuffs): Pre-existing modifiers on entity
        /// - Skill-specific: Skill grants bonus/penalty (future)
        /// </summary>
        public static List<AttributeModifier> GatherAttackModifiers(
            Entity attacker,
            VehicleComponent sourceComponent = null,
            Skill skill = null)
        {
            var modifiers = new List<AttributeModifier>();
            
            // 1. Intrinsic: Weapon enhancement bonus
            GatherWeaponBonus(sourceComponent, modifiers);
            
            // 2. Intrinsic: Character skill bonus
            GatherCharacterBonus(sourceComponent, modifiers);
            
            // 3. Applied: Status effects and equipment (shared helper)
            if (attacker != null)
            {
                D20RollHelpers.GatherAppliedModifiers(attacker, Attribute.AttackBonus, modifiers);
            }
            
            // 4. Skill-specific modifiers (future)
            if (skill != null)
            {
                GatherSkillModifiers(skill, modifiers);
            }
            
            return modifiers;
        }
        
        // ==================== MODIFIER SOURCES ====================
        
        /// <summary>
        /// Intrinsic: Weapon's inherent accuracy bonus.
        /// </summary>
        private static void GatherWeaponBonus(VehicleComponent sourceComponent, List<AttributeModifier> modifiers)
        {
            if (sourceComponent is WeaponComponent weapon && weapon.attackBonus != 0)
            {
                modifiers.Add(new AttributeModifier(
                    Attribute.AttackBonus,
                    ModifierType.Flat,
                    weapon.attackBonus,
                    weapon));
            }
        }
        
        /// <summary>
        /// Intrinsic: Character's base attack bonus (skill).
        /// </summary>
        private static void GatherCharacterBonus(VehicleComponent sourceComponent, List<AttributeModifier> modifiers)
        {
            if (sourceComponent?.assignedCharacter != null)
            {
                int charBonus = sourceComponent.assignedCharacter.baseAttackBonus;
                if (charBonus != 0)
                {
                    modifiers.Add(new AttributeModifier(
                        Attribute.AttackBonus,
                        ModifierType.Flat,
                        charBonus,
                        sourceComponent.assignedCharacter));
                }
            }
        }
        
        /// <summary>
        /// Skill-specific: Skill grants attack bonus/penalty (e.g., "Power Attack").
        /// </summary>
        private static void GatherSkillModifiers(Skill skill, List<AttributeModifier> modifiers)
        {
            // Future: Skill-specific attack bonuses
        }
        
        // ==================== RESULT HELPERS ====================
        
        /// <summary>
        /// Check if an attack roll is within the critical threat range.
        /// Currently only natural 20, but expandable to 19-20, 18-20, etc.
        /// </summary>
        public static bool IsCriticalThreat(AttackResult result, int criticalRange = 20)
        {
            return result.baseRoll >= criticalRange;
        }
        
        private static void AddPenalty(AttackResult result, int penalty, string reason)
        {
            if (penalty != 0)
            {
                result.modifiers.Add(new AttributeModifier(
                    Attribute.AttackBonus,
                    ModifierType.Flat,
                    -penalty,
                    null));
            }
        }
        
        // ==================== CRIT/FUMBLE ====================
        
        /// <summary>Check if this is a natural 20 (critical hit potential).</summary>
        public static bool IsNatural20(AttackResult result) => D20RollHelpers.IsNatural20(result);
        
        /// <summary>Check if this is a natural 1 (automatic miss).</summary>
        public static bool IsNatural1(AttackResult result) => D20RollHelpers.IsNatural1(result);
    }
}
