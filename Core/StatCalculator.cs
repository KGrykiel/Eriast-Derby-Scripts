using System.Collections.Generic;
using Assets.Scripts.Modifiers.DynamicModifiers;
using UnityEngine;

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
    /// INTEGER-FIRST DESIGN (CRPG Standard):
    /// - All base stats are integers (Speed: 40, AC: 18, HP: 100)
    /// - Calculations use float internally for multipliers
    /// - Final result rounded once and returned as int
    /// - Matches D&D/BG3/WOTR discrete value philosophy
    /// 
    /// Modifier sources (all stored in Entity.entityModifiers):
    /// - Status effect modifiers (Category = StatusEffect)
    /// - Cross-component modifiers (Category = Equipment)
    /// - Dynamic modifiers (Category = Dynamic) - calculated on-demand, not stored
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
        /// INTEGER-FIRST DESIGN:
        /// - Takes int baseValue
        /// - Uses float internally for multiplier calculations
        /// - Returns int (rounded once at the end)
        /// 
        /// All modifiers are stored in entity.entityModifiers:
        /// - Status effects add modifiers with Category = StatusEffect
        /// - Cross-component bonuses add modifiers with Category = Equipment
        /// - Dynamic modifiers calculated on-demand with Category = Dynamic
        /// 
        /// Returns: (final value, base value, modifier list for tooltips)
        /// Base value is returned separately - it's NOT a modifier.
        /// </summary>
        public static (int total, int baseValue, List<AttributeModifier> modifiers) GatherAttributeValueWithBreakdown(
            Entity entity,
            Attribute attribute,
            int baseValue)
        {
            var modifiers = new List<AttributeModifier>();
            
            if (entity == null)
            {
                return (baseValue, baseValue, modifiers);
            }
            
            // Gather all modifiers from entity's modifier list
            // This includes both status effect modifiers and cross-component equipment modifiers
            GatherEntityModifiers(entity, attribute, modifiers);
            
            // DYNAMIC MODIFIERS: Evaluate formulas on-demand (comment out this line to disable)
            modifiers.AddRange(DynamicModifierEvaluator.EvaluateAll(entity, attribute));
            
            // Calculate total: base + flat modifiers, then apply multipliers
            // Uses float internally, rounds once at end
            int total = CalculateTotal(baseValue, modifiers);
            
            return (total, baseValue, modifiers);
        }
        
        /// <summary>
        /// Convenience method for just getting the final value without breakdown.
        /// Use this when you don't need tooltip data.
        /// </summary>
        public static int GatherAttributeValue(Entity entity, Attribute attribute, int baseValue)
        {
            var (total, _, _) = GatherAttributeValueWithBreakdown(entity, attribute, baseValue);
            return total;
        }
        
        /// <summary>
        /// Gather defense value (AC) with breakdown - delegates to GatherAttributeValueWithBreakdown.
        /// Convenience method for attack/defense calculations.
        /// Returns int (already integer-first design).
        /// </summary>
        public static (int total, int baseValue, List<AttributeModifier> modifiers) GatherDefenseValueWithBreakdown(
            Entity target)
        {
            if (target == null)
            {
                return (10, 10, new List<AttributeModifier>());
            }
            
            // Delegate to main method (already returns int)
            return GatherAttributeValueWithBreakdown(
                target, 
                Attribute.ArmorClass, 
                target.GetBaseArmorClass());
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
        /// 
        /// Application order (D&D standard):
        /// 1. Start with base value (int)
        /// 2. Add all Flat modifiers
        /// 3. Apply all Multiplier modifiers
        /// 4. Round result once at end
        /// 
        /// INTEGER-FIRST: Uses float internally for multipliers, rounds once at end.
        /// Example: Base 30, +5 flat, ×1.15 multiplier = (30+5)×1.15 = 40.25 → 40
        /// </summary>
        private static int CalculateTotal(int baseValue, List<AttributeModifier> modifiers)
        {
            // Use float for internal calculations (handles multipliers)
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
            
            // Round once at the end (CRPG standard)
            return Mathf.RoundToInt(total);
        }
    }
}
