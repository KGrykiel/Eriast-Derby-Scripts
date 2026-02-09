using System.Collections.Generic;
using Assets.Scripts.Modifiers.DynamicModifiers;
using UnityEngine;

namespace Assets.Scripts.Core
{
    /// <summary>
    /// Central calculator for all stat/attribute value calculations with modifiers.
    /// 
    /// Single source of truth for stat calculations across the game.
    /// Entities store raw base values (dumb data), this gathers modifiers and computes finals.
    /// All methods are pure — they return data, never mutate inputs.
    /// 
    /// INTEGER-FIRST DESIGN (CRPG Standard):
    /// - All base stats are integers (Speed: 40, AC: 18, HP: 100)
    /// - Calculations use float internally for multipliers
    /// - Final result rounded once and returned as int
    /// </summary>
    public static class StatCalculator
    {
        /// <summary>
        /// Get an entity's attribute value with full modifier breakdown for tooltips.
        /// Returns: (final value, base value, modifier list)
        /// </summary>
        public static (int total, int baseValue, List<AttributeModifier> modifiers) GatherAttributeValueWithBreakdown(
            Entity entity,
            Attribute attribute,
            int baseValue)
        {
            if (entity == null)
            {
                return (baseValue, baseValue, new List<AttributeModifier>());
            }
            
            var entityModifiers = GatherEntityModifiers(entity, attribute);
            var dynamicModifiers = DynamicModifierEvaluator.EvaluateAll(entity, attribute);
            
            var allModifiers = new List<AttributeModifier>(entityModifiers.Count + dynamicModifiers.Count);
            allModifiers.AddRange(entityModifiers);
            allModifiers.AddRange(dynamicModifiers);
            
            int total = CalculateTotal(baseValue, allModifiers);
            
            return (total, baseValue, allModifiers);
        }
        
        /// <summary>
        /// Get the final value without breakdown. Use when you don't need tooltip data.
        /// </summary>
        public static int GatherAttributeValue(Entity entity, Attribute attribute, int baseValue)
        {
            var (total, _, _) = GatherAttributeValueWithBreakdown(entity, attribute, baseValue);
            return total;
        }
        
        /// <summary>
        /// Get defense value (AC) with breakdown.
        /// </summary>
        public static (int total, int baseValue, List<AttributeModifier> modifiers) GatherDefenseValueWithBreakdown(
            Entity target)
        {
            if (target == null)
            {
                return (10, 10, new List<AttributeModifier>());
            }
            
            return GatherAttributeValueWithBreakdown(
                target, 
                Attribute.ArmorClass, 
                target.GetBaseArmorClass());
        }
        
        // ==================== PRIVATE ====================
        
        /// <summary>
        /// Gather matching modifiers from an entity's modifier list.
        /// Returns a new list — does not mutate the entity.
        /// </summary>
        private static List<AttributeModifier> GatherEntityModifiers(Entity entity, Attribute attribute)
        {
            var modifiers = new List<AttributeModifier>();
            
            foreach (var mod in entity.GetModifiers())
            {
                if (mod.Attribute == attribute && mod.Value != 0)
                {
                    modifiers.Add(mod);
                }
            }
            
            return modifiers;
        }
        
        /// <summary>
        /// Calculate total value from base + modifiers.
        /// Application order: base → flat modifiers → multiplier modifiers → round once.
        /// </summary>
        private static int CalculateTotal(int baseValue, List<AttributeModifier> modifiers)
        {
            float total = baseValue;
            
            foreach (var mod in modifiers)
            {
                if (mod.Type == ModifierType.Flat)
                    total += mod.Value;
            }
            
            foreach (var mod in modifiers)
            {
                if (mod.Type == ModifierType.Multiplier)
                    total *= mod.Value;
            }
            
            return Mathf.RoundToInt(total);
        }
    }
}
