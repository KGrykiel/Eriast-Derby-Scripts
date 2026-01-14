using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Assets.Scripts.Entities.Vehicle.VehicleComponents.ComponentTypes;

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
    /// Used by:
    /// - AttackCalculator (for AC/defense values)
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
        /// Sources gathered:
        /// - Entity modifiers (from entityModifiers list, includes status effect modifiers)
        /// - Component bonuses (for vehicle components from other components)
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
            
            // 1. Gather entity modifiers (includes status effect modifiers - AppliedStatusEffect.OnApply() adds them)
            GatherEntityModifiers(entity, attribute, modifiers);
            
            // 2. Gather component bonuses (for vehicle components from other components)
            if (entity is VehicleComponent component && component.ParentVehicle != null)
            {
                GatherComponentBonuses(component.ParentVehicle, attribute, modifiers);
            }
            
            // 3. Calculate total: base + flat modifiers, then apply multipliers
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
                target.armorClass);
            
            return ((int)total, baseVal, modifiers);
        }
        
        /// <summary>
        /// Convenience method for just getting the defense value (AC).
        /// </summary>
        public static int GatherDefenseValue(Entity target)
        {
            var (total, _, _) = GatherDefenseValueWithBreakdown(target);
            return total;
        }
        
        // ==================== PRIVATE MODIFIER GATHERING ====================
        
        /// <summary>
        /// Gather modifiers from entity's entityModifiers list.
        /// This includes both direct modifiers AND modifiers created by status effects
        /// (AppliedStatusEffect.OnApply() adds them to entityModifiers).
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
        /// Gather bonuses from other vehicle components (equipment bonuses).
        /// Example: Shield generator provides +3 AC to chassis.
        /// </summary>
        private static void GatherComponentBonuses(
            Vehicle vehicle,
            Attribute attribute,
            List<AttributeModifier> modifiers)
        {
            // Map attributes to VehicleStatModifiers stat names
            var statName = MapAttributeToStatName(attribute);
            if (statName == null) return;
            
            float componentBonus = vehicle.GetComponentStat(statName);
            if (componentBonus != 0)
            {
                var mod = new AttributeModifier(
                    attribute,
                    ModifierType.Flat,
                    componentBonus,
                    vehicle,
                    ModifierCategory.Equipment
                );
                
                modifiers.Add(mod);
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
        
        /// <summary>
        /// Map Attribute enum to VehicleStatModifiers stat name strings.
        /// Only maps attributes that are supported by VehicleStatModifiers.
        /// </summary>
        private static string MapAttributeToStatName(Attribute attribute)
        {
            return attribute switch
            {
                Attribute.ArmorClass => VehicleStatModifiers.StatNames.AC,
                Attribute.Speed => VehicleStatModifiers.StatNames.Speed,
                Attribute.MaxHealth => VehicleStatModifiers.StatNames.HP,
                // Add more mappings as VehicleStatModifiers expands
                _ => null // Unsupported attributes return null
            };
        }
    }
}
