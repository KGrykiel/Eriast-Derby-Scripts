using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets.Scripts.Combat.Attacks
{
    /// <summary>
    /// Central calculator for all attack roll logic.
    /// 
    /// Responsibilities:
    /// - Rolling d20 attacks (uses RollUtility)
    /// - Gathering attack modifiers from all sources
    /// - Gathering defense values from all sources (with breakdown for tooltips)
    /// - Evaluating hit/miss
    /// - Crit/fumble detection
    /// 
    /// DESIGN: Entities store raw base values. This calculator gathers modifiers
    /// from all sources and computes final values. This keeps entities clean
    /// and provides breakdown data for tooltips.
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
            
            // Get target's defense value and evaluate
            int defenseValue = GatherDefenseValue(target);
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
        /// </summary>
        public static List<AttackModifier> GatherAttackModifiers(
            Entity attacker,
            VehicleComponent sourceComponent = null,
            Skill skill = null)
        {
            var modifiers = new List<AttackModifier>();
            
            // 1. Weapon attack bonus
            if (sourceComponent is WeaponComponent weapon && weapon.attackBonus != 0)
            {
                modifiers.Add(new AttackModifier("Weapon Bonus", weapon.attackBonus, weapon.name));
            }
            
            // 2. Vehicle/Character base attack bonus
            Vehicle attackerVehicle = GetVehicleFromEntity(attacker);
            if (attackerVehicle != null)
            {
                int vehicleBonus = GetVehicleAttackBonus(attackerVehicle);
                if (vehicleBonus != 0)
                {
                    modifiers.Add(new AttackModifier("Vehicle Bonus", vehicleBonus, attackerVehicle.vehicleName));
                }
            }
            
            // 3. Status effect attack modifiers
            if (attacker != null)
            {
                GatherStatusEffectModifiers(attacker, Attribute.AttackBonus, modifiers);
            }
            
            // 4. Component-based attack modifiers
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
        
        // ==================== DEFENSE VALUE GATHERING ====================
        
        /// <summary>
        /// Gather target's effective defense value (AC) from all sources.
        /// Returns just the total - use GatherDefenseValueWithBreakdown() for tooltip data.
        /// </summary>
        public static int GatherDefenseValue(Entity target, string defenseType = "AC")
        {
            var (total, _) = GatherDefenseValueWithBreakdown(target, defenseType);
            return total;
        }
        
        /// <summary>
        /// Gather target's effective defense value with full breakdown for tooltips.
        /// This is the SINGLE SOURCE OF TRUTH for defense values.
        /// 
        /// Sources gathered:
        /// - Base AC (from Entity.armorClass)
        /// - Entity modifiers (from entityModifiers list)
        /// - Status effect modifiers (Attribute.ArmorClass)
        /// - Component bonuses (for chassis - from other vehicle components)
        /// - Situational modifiers (cover, elevation, etc.)
        /// </summary>
        public static (int total, List<AttackModifier> breakdown) GatherDefenseValueWithBreakdown(
            Entity target, 
            string defenseType = "AC")
        {
            var breakdown = new List<AttackModifier>();
            
            if (target == null)
            {
                breakdown.Add(new AttackModifier("Default", 10, "No Target"));
                return (10, breakdown);
            }
            
            // 1. Base AC from entity
            int baseAC = target.armorClass;
            breakdown.Add(new AttackModifier("Base", baseAC, target.GetDisplayName()));
            
            // 2. Entity's direct modifiers (from entityModifiers list)
            foreach (var mod in target.GetModifiers())
            {
                if (mod.Attribute == Attribute.ArmorClass)
                {
                    int value = mod.Type == ModifierType.Flat 
                        ? (int)mod.Value 
                        : Mathf.RoundToInt(baseAC * mod.Value / 100f);
                    
                    if (value != 0)
                    {
                        breakdown.Add(new AttackModifier(
                            mod.SourceDisplayName,
                            value,
                            mod.Source?.name ?? "Equipment"));
                    }
                }
            }
            
            // 3. Status effect modifiers
            foreach (var applied in target.GetActiveStatusEffects())
            {
                foreach (var mod in applied.template.modifiers)
                {
                    if (mod.attribute == Attribute.ArmorClass)
                    {
                        int value = (int)mod.value;
                        if (value != 0)
                        {
                            breakdown.Add(new AttackModifier(
                                applied.template.effectName,
                                value,
                                applied.template.effectName));
                        }
                    }
                }
            }
            
            // 4. Component bonuses (for chassis - other components providing AC)
            if (target is ChassisComponent chassis && chassis.ParentVehicle != null)
            {
                float componentBonus = chassis.ParentVehicle.GetComponentStat(VehicleStatModifiers.StatNames.AC);
                if (componentBonus != 0)
                {
                    breakdown.Add(new AttackModifier(
                        "Component Bonus",
                        (int)componentBonus,
                        "Vehicle Components"));
                }
            }
            
            // 5. Situational modifiers (cover, elevation, etc.)
            int situational = GatherSituationalDefenseModifiers(target);
            if (situational != 0)
            {
                breakdown.Add(new AttackModifier("Situational", situational, "Environment"));
            }
            
            // Calculate total
            int total = breakdown.Sum(m => m.value);
            
            return (total, breakdown);
        }
        
        // ==================== MODIFIER SOURCES ====================
        
        private static int GetVehicleAttackBonus(Vehicle vehicle)
        {
            // Future: Pull from pilot stats, vehicle upgrades, etc.
            return 0;
        }
        
        private static void GatherStatusEffectModifiers(Entity entity, Attribute attribute, List<AttackModifier> modifiers)
        {
            foreach (var applied in entity.GetActiveStatusEffects())
            {
                foreach (var mod in applied.template.modifiers)
                {
                    if (mod.attribute == attribute)
                    {
                        int value = (int)mod.value;
                        if (value != 0)
                        {
                            modifiers.Add(new AttackModifier(
                                applied.template.effectName,
                                value,
                                applied.template.effectName));
                        }
                    }
                }
            }
        }
        
        private static void GatherComponentAttackModifiers(Vehicle vehicle, VehicleComponent excludeComponent, List<AttackModifier> modifiers)
        {
            // Future: Check for components that provide attack bonuses
            // e.g., targeting systems, fire control computers
        }
        
        private static void GatherSkillAttackModifiers(Skill skill, List<AttackModifier> modifiers)
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
                result.modifiers.Add(new AttackModifier(name, value, source));
            }
        }
        
        /// <summary>
        /// Add a modifier conditionally.
        /// </summary>
        public static void AddModifierIf(AttackResult result, bool condition, string name, int value, string source = null)
        {
            if (condition && value != 0)
            {
                result.modifiers.Add(new AttackModifier(name, value, source));
            }
        }
        
        /// <summary>
        /// Add multiple modifiers from a list.
        /// </summary>
        public static void AddModifiers(AttackResult result, IEnumerable<AttackModifier> modifiers)
        {
            if (modifiers == null) return;
            foreach (var mod in modifiers)
            {
                if (mod.value != 0)
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
        private readonly List<AttackModifier> modifiers = new List<AttackModifier>();
        
        public AttackModifierBuilder Add(string name, int value, string source = null)
        {
            if (value != 0)
            {
                modifiers.Add(new AttackModifier(name, value, source));
            }
            return this;
        }
        
        public AttackModifierBuilder AddIf(bool condition, string name, int value, string source = null)
        {
            if (condition && value != 0)
            {
                modifiers.Add(new AttackModifier(name, value, source));
            }
            return this;
        }
        
        public List<AttackModifier> Build() => modifiers;
        
        public static implicit operator List<AttackModifier>(AttackModifierBuilder builder) => builder.Build();
    }
}
