using System.Collections.Generic;
using Core;

namespace Combat
{
    /// <summary>
    /// Shared helper methods for all d20 roll calculators.
    /// Eliminates duplication between AttackCalculator, SaveCalculator, and future calculators.
    /// 
    /// Used by:
    /// - AttackCalculator (attack rolls)
    /// - SaveCalculator (saving throws)
    /// - Future: SkillCheckCalculator, OpposedCheckCalculator
    /// </summary>
    public static class D20RollHelpers
    {
        // ==================== MODIFIER OPERATIONS ====================
        
        /// <summary>
        /// Add modifiers to a d20 roll result.
        /// Shared by all calculators - applies modifiers to any roll type.
        /// </summary>
        public static void AddModifiers(ID20RollResult result, List<AttributeModifier> modifiers)
        {
            foreach (var mod in modifiers)
            {
                if (mod.Value != 0)
                {
                    result.modifiers.Add(mod);
                }
            }
        }
        
        /// <summary>
        /// Gather applied modifiers (status effects, equipment) from an entity.
        /// This is the "applied modifiers" step shared by all d20 calculators.
        /// 
        /// Delegates to StatCalculator (single source of truth) to get modifiers
        /// that are already on the entity.
        /// </summary>
        public static void GatherAppliedModifiers(
            Entity entity,
            Attribute attribute,
            List<AttributeModifier> modifiers)
        {
            if (entity == null) return;
            
            var (_, _, appliedMods) = StatCalculator.GatherAttributeValueWithBreakdown(
                entity, attribute, 0f);
            
            foreach (var mod in appliedMods)
            {
                if (mod.Value != 0)
                {
                    modifiers.Add(mod);
                }
            }
        }
        
        // ==================== EVALUATION ====================
        
        /// <summary>
        /// Evaluate d20 roll against target number.
        /// Success if Total >= targetValue.
        /// Shared by all calculators - same logic for attacks, saves, checks.
        /// </summary>
        public static void EvaluateAgainstTarget(ID20RollResult result, int targetValue)
        {
            result.targetValue = targetValue;
            result.success = result.Total >= targetValue;
        }
        
        // ==================== SPECIAL ROLLS ====================
        
        /// <summary>
        /// Check if this is a natural 20 (critical success potential).
        /// </summary>
        public static bool IsNatural20(ID20RollResult result) => result.baseRoll == 20;
        
        /// <summary>
        /// Check if this is a natural 1 (critical failure potential).
        /// </summary>
        public static bool IsNatural1(ID20RollResult result) => result.baseRoll == 1;
    }
}

