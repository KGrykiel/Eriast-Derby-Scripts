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
        /// - Base value (from entity or component)
        /// - Entity modifiers (from entityModifiers list)
        /// - Status effect modifiers (from activeStatusEffects)
        /// - Component bonuses (for vehicle components from other components)
        /// 
        /// Returns: (final value, breakdown list for tooltips)
        /// </summary>
        public static (float total, List<AttributeModifier> breakdown) GatherAttributeValueWithBreakdown(
            Entity entity,
            Attribute attribute,
            float baseValue)
        {
            var breakdown = new List<AttributeModifier>();
            
            if (entity == null)
            {
                breakdown.Add(new AttributeModifier(attribute, ModifierType.Flat, baseValue, null));
                return (baseValue, breakdown);
            }
            
            // 1. Base value
            breakdown.Add(new AttributeModifier(attribute, ModifierType.Flat, baseValue, entity as UnityEngine.Object));
            
            // 2. Entity's direct modifiers (from entityModifiers list)
            GatherEntityModifiers(entity, attribute, baseValue, breakdown);
            
            // 3. Status effect modifiers (from activeStatusEffects)
            GatherStatusEffectModifiers(entity, attribute, baseValue, breakdown);
            
            // 4. Component bonuses (for vehicle components from other components)
            if (entity is VehicleComponent component && component.ParentVehicle != null)
            {
                GatherComponentBonuses(component.ParentVehicle, attribute, breakdown);
            }
            
            // Calculate total
            float total = breakdown.Sum(m => m.Type == ModifierType.Flat ? m.Value : baseValue * m.Value / 100f);
            
            return (total, breakdown);
        }
        
        /// <summary>
        /// Convenience method for just getting the final value without breakdown.
        /// Use this when you don't need tooltip data.
        /// </summary>
        public static float GatherAttributeValue(Entity entity, Attribute attribute, float baseValue)
        {
            var (total, _) = GatherAttributeValueWithBreakdown(entity, attribute, baseValue);
            return total;
        }
        
        /// <summary>
        /// Gather defense value (AC) with breakdown - delegates to GatherAttributeValueWithBreakdown.
        /// Convenience method for attack/defense calculations.
        /// </summary>
        public static (int total, List<AttributeModifier> breakdown) GatherDefenseValueWithBreakdown(
            Entity target,
            string defenseType = "AC")
        {
            if (target == null)
            {
                var defaultBreakdown = new List<AttributeModifier> 
                { 
                    new AttributeModifier(Attribute.ArmorClass, ModifierType.Flat, 10, null)
                };
                return (10, defaultBreakdown);
            }
            
            // Delegate to main method
            var (total, breakdown) = GatherAttributeValueWithBreakdown(
                target, 
                Attribute.ArmorClass, 
                target.armorClass);
            
            return ((int)total, breakdown);
        }
        
        /// <summary>
        /// Convenience method for just getting the defense value (AC).
        /// </summary>
        public static int GatherDefenseValue(Entity target)
        {
            var (total, _) = GatherDefenseValueWithBreakdown(target);
            return total;
        }
        
        // ==================== PRIVATE MODIFIER GATHERING ====================
        
        /// <summary>
        /// Gather modifiers from entity's entityModifiers list.
        /// </summary>
        private static void GatherEntityModifiers(
            Entity entity,
            Attribute attribute,
            float baseValue,
            List<AttributeModifier> breakdown)
        {
            foreach (var mod in entity.GetModifiers())
            {
                if (mod.Attribute != attribute) continue;
                
                if (mod.Value != 0)
                {
                    breakdown.Add(mod);
                }
            }
        }
        
        /// <summary>
        /// Gather modifiers from entity's active status effects.
        /// </summary>
        private static void GatherStatusEffectModifiers(
            Entity entity,
            Attribute attribute,
            float baseValue,
            List<AttributeModifier> breakdown)
        {
            foreach (var applied in entity.GetActiveStatusEffects())
            {
                foreach (var modData in applied.template.modifiers)
                {
                    if (modData.attribute != attribute) continue;
                    
                    if (modData.value != 0)
                    {
                        // Convert ModifierData to AttributeModifier
                        var mod = new AttributeModifier(
                            modData.attribute,
                            modData.type,
                            modData.value,
                            applied.template);
                        
                        breakdown.Add(mod);
                    }
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
            List<AttributeModifier> breakdown)
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
                    vehicle);
                
                breakdown.Add(mod);
            }
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
