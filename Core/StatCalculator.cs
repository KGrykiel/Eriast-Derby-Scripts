using System.Collections.Generic;

namespace Assets.Scripts.Core
{
    /// <summary>
    /// Central calculator for all stat/attribute value calculations with modifiers.
    /// 
    /// This is THE single source of truth for stat calculations across the game.
    /// Follows the calculator-centric pattern from Session 9:
    /// - Entities store raw base values (dumb data)
    /// - StatCalculator gathers modifiers from all sources and computes final values
    /// - Returns breakdown data for tooltips
    /// 
    /// Modifier sources (all stored in Entity.entityModifiers):
    /// - Status effect modifiers (Category = StatusEffect)
    /// - Cross-component modifiers (Category = Equipment)
    /// 
    /// Used by:
    /// - CombatLogManager (for tooltip formatting)
    /// - UI components (for stat display with tooltips)
    /// - Vehicle/Component systems (for modified stat values)
    /// </summary>
    public static class StatCalculator
    {
        // ==================== MAIN PUBLIC API ====================
        
        /// <summary>
        /// Gather an entity's attribute value with full modifier breakdown for tooltips.
        /// This is the SINGLE SOURCE OF TRUTH for stat calculations.
        /// 
        /// All modifiers are stored in entity.entityModifiers:
        /// - Status effects add modifiers with Category = StatusEffect
        /// - Cross-component bonuses add modifiers with Category = Equipment
        /// 
        /// Returns: (final value, base value, modifier list for tooltips)
        /// Base value is returned separately - it's NOT a modifier.
        /// </summary>
        public static (float total, float baseValue, List<AttributeModifier> modifiers) GatherAttributeValueWithBreakdown(
            Entity entity,
            Attribute attribute,
            float baseValue)
        {
            var modifiers = new List<AttributeModifier>();
            
            if (entity == null)
            {
                return (baseValue, baseValue, modifiers);
            }
            
            // Gather all modifiers from entity's modifier list
            // This includes both status effect modifiers and cross-component equipment modifiers
            GatherEntityModifiers(entity, attribute, modifiers);
            
            // Calculate total: base + flat modifiers, then apply multipliers
            float total = CalculateTotal(baseValue, modifiers);
            
            return (total, baseValue, modifiers);
        }
        
        /// <summary>
        /// Convenience method for just getting the final value without breakdown.
        /// Use this when you don't need tooltip data.
        /// </summary>
        public static float GatherAttributeValue(Entity entity, Attribute attribute, float baseValue)
        {
            var (total, _, _) = GatherAttributeValueWithBreakdown(entity, attribute, baseValue);
            return total;
        }
        
        /// <summary>
        /// Gather defense value (AC) with breakdown - delegates to GatherAttributeValueWithBreakdown.
        /// Convenience method for attack/defense calculations.
        /// </summary>
        public static (int total, float baseValue, List<AttributeModifier> modifiers) GatherDefenseValueWithBreakdown(
            Entity target,
            string defenseType = "AC")
        {
            if (target == null)
            {
                return (10, 10f, new List<AttributeModifier>());
            }
            
            // Delegate to main method
            var (total, baseVal, modifiers) = GatherAttributeValueWithBreakdown(
                target, 
                Attribute.ArmorClass, 
                target.GetBaseArmorClass());
            
            return ((int)total, baseVal, modifiers);
        }
        // ==================== PRIVATE MODIFIER GATHERING ====================
        
        /// <summary>
        /// Gather modifiers from entity's entityModifiers list.
        /// This includes:
        /// - Status effect modifiers (Category = StatusEffect)
        /// - Cross-component equipment modifiers (Category = Equipment)
        /// </summary>
        private static void GatherEntityModifiers(
            Entity entity,
            Attribute attribute,
            List<AttributeModifier> modifiers)
        {
            foreach (var mod in entity.GetModifiers())
            {
                if (mod.Attribute != attribute) continue;
                
                if (mod.Value != 0)
                {
                    modifiers.Add(mod);
                }
            }
        }
        
        /// <summary>
        /// Calculate total value from base + modifiers.
        /// Application order (D&D standard):
        /// 1. Start with base value
        /// 2. Add all Flat modifiers
        /// 3. Apply all Multiplier modifiers
        /// </summary>
        private static float CalculateTotal(float baseValue, List<AttributeModifier> modifiers)
        {
            float total = baseValue;
            
            // Step 1: Apply flat modifiers
            foreach (var mod in modifiers)
            {
                if (mod.Type == ModifierType.Flat)
                {
                    total += mod.Value;
                }
            }
            
            // Step 2: Apply multiplier modifiers
            foreach (var mod in modifiers)
            {
                if (mod.Type == ModifierType.Multiplier)
                {
                    total *= mod.Value;
                }
            }
            
            return total;
        }
    }
}
