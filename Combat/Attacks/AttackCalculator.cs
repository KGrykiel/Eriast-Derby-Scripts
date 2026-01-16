using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Core;

namespace Combat.Attacks
{
    /// <summary>
    /// Central calculator for all attack roll logic.
    /// 
    /// Responsibilities:
    /// - Rolling d20 attacks (uses RollUtility)
    /// - Gathering attack modifiers from all sources
    /// - Evaluating hit/miss
    /// - Crit/fumble detection
    /// 
    /// DESIGN: Entities store raw base values. This calculator gathers modifiers
    /// from all sources and computes final values. This keeps entities clean
    /// and provides breakdown data for tooltips.
    /// 
    /// NOTE: Defense values (AC) are now delegated to StatCalculator (single source of truth).
    /// </summary>
    public static class AttackCalculator
    {
        // ==================== ATTACK ROLLING ====================
        
        /// <summary>
        /// Roll a d20 attack and create an attack result.
        /// Uses RollUtility for the actual die roll.
        /// </summary>
        public static AttackResult RollAttack(AttackCategory category = AttackCategory.Attack)
        {
            int roll = RollUtility.RollD20();
            return AttackResult.FromD20(roll, category);
        }
        
        /// <summary>
        /// Create an attack result from a specific roll value (for testing/predetermined rolls).
        /// </summary>
        public static AttackResult FromRoll(int baseRoll, AttackCategory category = AttackCategory.Attack)
        {
            return AttackResult.FromD20(baseRoll, category);
        }
        
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
            var result = RollAttack(AttackCategory.Attack);
            
            // Gather and add attack modifiers
            var modifiers = GatherAttackModifiers(attacker, sourceComponent, skill);
            AddModifiers(result, modifiers);
            
            // Add any additional penalty (e.g., component targeting)
            if (additionalPenalty != 0)
            {
                AddModifier(result, "Targeting Penalty", -additionalPenalty, skill?.name);
            }
            
            // Get target's defense value and evaluate (use StatCalculator directly)
            int defenseValue = StatCalculator.GatherDefenseValue(target);
            EvaluateAgainst(result, defenseValue, "AC");
            
            return result;
        }
        
        /// <summary>
        /// Perform a skill check (d20 vs DC).
        /// </summary>
        public static AttackResult PerformSkillCheck(
            Entity checker,
            int difficulty,
            string checkName = "Skill Check")
        {
            var result = RollAttack(AttackCategory.SkillCheck);
            
            // Future: gather skill check modifiers
            // var modifiers = GatherSkillCheckModifiers(checker, checkName);
            // AddModifiers(result, modifiers);
            
            EvaluateAgainst(result, difficulty, "DC");
            
            return result;
        }
        
        // ==================== ATTACK MODIFIER GATHERING ====================
        
        /// <summary>
        /// Gather ALL attack modifiers from all sources.
        /// This is the SINGLE SOURCE OF TRUTH for attack bonuses.
        /// Returns list of AttributeModifiers for the AttackBonus attribute.
        /// </summary>
        public static List<AttributeModifier> GatherAttackModifiers(
            Entity attacker,
            VehicleComponent sourceComponent = null,
            Skill skill = null)
        {
            var modifiers = new List<AttributeModifier>();
            
            // 1. Weapon enhancement bonus (inherent weapon accuracy)
            if (sourceComponent is WeaponComponent weapon && weapon.attackBonus != 0)
            {
                modifiers.Add(new AttributeModifier(
                    Attribute.AttackBonus,
                    ModifierType.Flat,
                    weapon.attackBonus,
                    weapon));
            }
            
            // 2. Character attack bonus (from component's assigned character)
            GatherCharacterAttackModifiers(sourceComponent, modifiers);
            
            // 3. Status effect attack modifiers
            if (attacker != null)
            {
                GatherStatusEffectModifiers(attacker, Attribute.AttackBonus, modifiers);
            }
            
            // 4. Component-based attack modifiers (e.g., targeting computers)
            Vehicle attackerVehicle = GetVehicleFromEntity(attacker);
            if (attackerVehicle != null)
            {
                GatherComponentAttackModifiers(attackerVehicle, sourceComponent, modifiers);
            }
            
            // 5. Skill-specific modifiers
            if (skill != null)
            {
                GatherSkillAttackModifiers(skill, modifiers);
            }
            
            return modifiers;
        }
        
        // ==================== MODIFIER SOURCES ====================
        
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
        
        private static void GatherStatusEffectModifiers(Entity entity, Attribute attribute, List<AttributeModifier> modifiers)
        {
            // Delegate to StatCalculator - single source of truth for modifier gathering
            // Get all modifiers for this attribute, then filter to only status effects
            var (_, _, allModifiers) = StatCalculator.GatherAttributeValueWithBreakdown(entity, attribute, 0f);
            
            // Only add status effect modifiers (exclude cross-component equipment modifiers)
            foreach (var mod in allModifiers)
            {
                if (mod.Category == ModifierCategory.StatusEffect && mod.Value != 0)
                {
                    modifiers.Add(mod);
                }
            }
        }
        
        private static void GatherComponentAttackModifiers(Vehicle vehicle, VehicleComponent excludeComponent, List<AttributeModifier> modifiers)
        {
            // Future: Check for components that provide attack bonuses
            // e.g., targeting systems, fire control computers
        }
        
        private static void GatherSkillAttackModifiers(Skill skill, List<AttributeModifier> modifiers)
        {
            // Future: Skill-specific attack bonuses
            // e.g., "Power Attack" adds bonus at cost of accuracy
        }
        
        private static int GatherSituationalDefenseModifiers(Entity target)
        {
            // Future: Cover bonuses, elevation, etc.
            return 0;
        }
        
        private static Vehicle GetVehicleFromEntity(Entity entity)
        {
            if (entity is VehicleComponent component)
            {
                return component.ParentVehicle;
            }
            return null;
        }
        
        // ==================== RESULT MODIFICATION ====================
        
        /// <summary>
        /// Add a modifier to an attack result.
        /// </summary>
        public static void AddModifier(AttackResult result, string name, int value, string source = null)
        {
            if (value != 0)
            {
                result.modifiers.Add(new AttributeModifier(
                    Attribute.AttackBonus,
                    ModifierType.Flat,
                    value,
                    source != null ? null : null)); // Source is a string, not UnityEngine.Object
            }
        }
        
        /// <summary>
        /// Add a modifier conditionally.
        /// </summary>
        public static void AddModifierIf(AttackResult result, bool condition, string name, int value, string source = null)
        {
            if (condition && value != 0)
            {
                result.modifiers.Add(new AttributeModifier(
                    Attribute.AttackBonus,
                    ModifierType.Flat,
                    value,
                    null));
            }
        }
        
        /// <summary>
        /// Add multiple modifiers from a list.
        /// </summary>
        public static void AddModifiers(AttackResult result, IEnumerable<AttributeModifier> modifiers)
        {
            if (modifiers == null) return;
            foreach (var mod in modifiers)
            {
                if (mod.Value != 0)
                {
                    result.modifiers.Add(mod);
                }
            }
        }
        
        // ==================== EVALUATION ====================
        
        /// <summary>
        /// Evaluate the roll against a target value (AC/DC).
        /// Sets success to true if Total >= targetValue.
        /// </summary>
        public static void EvaluateAgainst(AttackResult result, int targetValue, string targetName = "AC")
        {
            result.targetValue = targetValue;
            result.targetName = targetName;
            result.success = result.Total >= targetValue;
        }
        
        /// <summary>
        /// Check if this is a natural 20 (critical hit potential).
        /// </summary>
        public static bool IsNatural20(AttackResult result) => result.baseRoll == 20;
        
        /// <summary>
        /// Check if this is a natural 1 (automatic miss).
        /// </summary>
        public static bool IsNatural1(AttackResult result) => result.baseRoll == 1;
        
        // ==================== FLUENT BUILDER ====================
        
        /// <summary>
        /// Create a modifier list builder for fluent API.
        /// </summary>
        public static AttackModifierBuilder BuildModifiers() => new AttackModifierBuilder();
    }
    
    /// <summary>
    /// Fluent builder for creating modifier lists.
    /// </summary>
    public class AttackModifierBuilder
    {
        private readonly List<AttributeModifier> modifiers = new List<AttributeModifier>();
        
        public AttackModifierBuilder Add(string name, int value, string source = null)
        {
            if (value != 0)
            {
                modifiers.Add(new AttributeModifier(
                    Attribute.AttackBonus,
                    ModifierType.Flat,
                    value,
                    null));
            }
            return this;
        }
        
        public AttackModifierBuilder AddIf(bool condition, string name, int value, string source = null)
        {
            if (condition && value != 0)
            {
                modifiers.Add(new AttributeModifier(
                    Attribute.AttackBonus,
                    ModifierType.Flat,
                    value,
                    null));
            }
            return this;
        }
        
        public List<AttributeModifier> Build() => modifiers;
        
        public static implicit operator List<AttributeModifier>(AttackModifierBuilder builder) => builder.Build();
    }
}
