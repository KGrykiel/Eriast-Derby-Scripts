using System.Collections.Generic;
using Assets.Scripts.Modifiers.DynamicModifiers;
using UnityEngine;

namespace Assets.Scripts.Core
{
    /// <summary>
    /// Single source of truth for all stat calculations.
    /// Entities store raw base values, this gathers modifiers and computes finals.
    /// All methods are pure — they return data, never mutate inputs.
    /// </summary>
    public static class StatCalculator
    {
        /// <summary>Returns (final, base, modifiers) — the breakdown variant is used for tooltips.</summary>
        public static (int total, int baseValue, List<AttributeModifier> modifiers) GatherAttributeValueWithBreakdown(
            Entity entity,
            Attribute attribute,
            int baseValue)
        {
            var entityModifiers = GatherEntityModifiers(entity, attribute);
            var dynamicModifiers = DynamicModifierEvaluator.EvaluateAll(entity, attribute);
            
            var allModifiers = new List<AttributeModifier>(entityModifiers.Count + dynamicModifiers.Count);
            allModifiers.AddRange(entityModifiers);
            allModifiers.AddRange(dynamicModifiers);
            
            int total = CalculateTotal(baseValue, allModifiers);
            
            return (total, baseValue, allModifiers);
        }
        
        /// <summary>Convenience overload when you don't need the breakdown.</summary>
        public static int GatherAttributeValue(Entity entity, Attribute attribute, int baseValue)
        {
            var (total, _, _) = GatherAttributeValueWithBreakdown(entity, attribute, baseValue);
            return total;
        }
        
        /// <summary>Convenience method to get AC</summary>
        public static (int total, int baseValue, List<AttributeModifier> modifiers) GatherDefenseValueWithBreakdown(
            Entity target)
        {
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
