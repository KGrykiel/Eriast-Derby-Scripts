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
            AddModifiers(result, modifiers);
            
            // Add any additional penalty (e.g., component targeting)
            if (additionalPenalty != 0)
            {
                AddPenalty(result, additionalPenalty, "Targeting Penalty");
            }
            
            // Get target's defense value and evaluate
            int defenseValue = StatCalculator.GatherDefenseValue(target);
            EvaluateAgainstAC(result, defenseValue);
            
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
            
            // 3. Applied: Status effects and equipment
            if (attacker != null)
            {
                GatherAppliedModifiers(attacker, modifiers);
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
        /// Applied: Status effects and equipment modifiers already on the entity.
        /// Delegates to StatCalculator (single source of truth).
        /// </summary>
        private static void GatherAppliedModifiers(Entity entity, List<AttributeModifier> modifiers)
        {
            var (_, _, allModifiers) = StatCalculator.GatherAttributeValueWithBreakdown(entity, Attribute.AttackBonus, 0f);
            
            foreach (var mod in allModifiers)
            {
                if (mod.Value != 0)
                {
                    modifiers.Add(mod);
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
        
        private static void AddModifiers(AttackResult result, List<AttributeModifier> modifiers)
        {
            foreach (var mod in modifiers)
            {
                if (mod.Value != 0)
                {
                    result.modifiers.Add(mod);
                }
            }
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
        
        private static void EvaluateAgainstAC(AttackResult result, int ac)
        {
            result.targetValue = ac;
            result.success = result.Total >= ac;
        }
        
        // ==================== CRIT/FUMBLE ====================
        
        /// <summary>Check if this is a natural 20 (critical hit potential).</summary>
        public static bool IsNatural20(AttackResult result) => result.baseRoll == 20;
        
        /// <summary>Check if this is a natural 1 (automatic miss).</summary>
        public static bool IsNatural1(AttackResult result) => result.baseRoll == 1;
    }
}
