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
            return AttackResult.FromD20(roll, AttackCategory.Attack);
        }
        
        // ==================== ATTACK MODIFIER GATHERING ====================
        
        /// <summary>
        /// Gather ALL attack modifiers from all sources.
        /// Single source of truth for attack bonuses.
        /// </summary>
        public static List<AttributeModifier> GatherAttackModifiers(
            Entity attacker,
            VehicleComponent sourceComponent = null,
            Skill skill = null)
        {
            var modifiers = new List<AttributeModifier>();
            
            // 1. Weapon enhancement bonus
            GatherWeaponAttackModifiers(sourceComponent, modifiers);
            
            // 2. Character attack bonus
            GatherCharacterAttackModifiers(sourceComponent, modifiers);
            
            // 3. Entity modifiers (status effects + equipment)
            if (attacker != null)
            {
                GatherEntityAttackModifiers(attacker, modifiers);
            }
            
            // 4. Skill-specific modifiers (future)
            if (skill != null)
            {
                GatherSkillAttackModifiers(skill, modifiers);
            }
            
            return modifiers;
        }
        
        // ==================== MODIFIER SOURCES ====================
        
        private static void GatherWeaponAttackModifiers(VehicleComponent sourceComponent, List<AttributeModifier> modifiers)
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
        
        private static void GatherCharacterAttackModifiers(VehicleComponent sourceComponent, List<AttributeModifier> modifiers)
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
        
        private static void GatherEntityAttackModifiers(Entity entity, List<AttributeModifier> modifiers)
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
        
        private static void GatherSkillAttackModifiers(Skill skill, List<AttributeModifier> modifiers)
        {
            // Future: Skill-specific attack bonuses (e.g., "Power Attack")
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
            result.targetName = "AC";
            result.success = result.Total >= ac;
        }
        
        // ==================== CRIT/FUMBLE ====================
        
        /// <summary>Check if this is a natural 20 (critical hit potential).</summary>
        public static bool IsNatural20(AttackResult result) => result.baseRoll == 20;
        
        /// <summary>Check if this is a natural 1 (automatic miss).</summary>
        public static bool IsNatural1(AttackResult result) => result.baseRoll == 1;
    }
}
