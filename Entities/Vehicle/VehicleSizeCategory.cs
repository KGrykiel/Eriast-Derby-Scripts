using System.Collections.Generic;

namespace Assets.Scripts.Entities.Vehicle
{
    /// <summary>
    /// Vehicle size categories for physics-based balancing.
    /// Based on total mass, determines AC, mobility, speed, and initiative modifiers.
    /// See SizeBalancing.md for complete design rationale.
    /// </summary>
    public enum VehicleSizeCategory
    {
        /// <summary>Tiny vehicle (solo speeder, &lt;50 mass). +3 AC, +3 Mobility, +5 Initiative, Fast.</summary>
        Tiny,
        
        /// <summary>Small vehicle (2-person light, 50-100 mass). +1 AC, +1 Mobility, +2 Initiative, Fast.</summary>
        Small,
        
        /// <summary>Medium vehicle (3-5 person standard, 100-200 mass). Baseline (0 modifiers).</summary>
        Medium,
        
        /// <summary>Large vehicle (4-person heavy, 200-400 mass). -2 AC, -3 Mobility, -3 Initiative, Slow.</summary>
        Large,
        
        /// <summary>Huge vehicle (oversized battle wagon, 400+ mass). -4 AC, -5 Mobility, -5 Initiative, Very Slow.</summary>
        Huge
    }
    
    /// <summary>
    /// Static utility for size-based modifier definitions.
    /// Returns AttributeModifier lists that automatically route to correct components.
    /// Modifiers use ModifierCategory.Equipment so they persist until vehicle size changes.
    /// </summary>
    public static class VehicleSizeModifiers
    {
        /// <summary>
        /// Get all attribute modifiers for a given vehicle size.
        /// These modifiers are applied to the vehicle and automatically route to correct components.
        /// 
        /// Returns modifiers for:
        /// - ArmorClass (routes to Chassis)
        /// - Mobility (routes to Chassis, used in saves)
        /// - MaxSpeed (routes to DriveComponent)
        /// - Initiative (vehicle-level stat)
        /// </summary>
        /// <param name="size">Vehicle size category</param>
        /// <param name="source">Source object for the modifiers (usually the Vehicle itself)</param>
        /// <returns>List of attribute modifiers to apply</returns>
        public static List<AttributeModifier> GetModifiers(VehicleSizeCategory size, UnityEngine.Object source)
        {
            var modifiers = new List<AttributeModifier>();
            
            // AC Modifier (routes to Chassis)
            // Large vehicles are easier to hit (bigger target)
            int acMod = size switch
            {
                VehicleSizeCategory.Tiny => 3,
                VehicleSizeCategory.Small => 1,
                VehicleSizeCategory.Medium => 0,
                VehicleSizeCategory.Large => -2,
                VehicleSizeCategory.Huge => -4,
                _ => 0
            };
            
            if (acMod != 0)
            {
                modifiers.Add(new AttributeModifier(
                    Attribute.ArmorClass,
                    ModifierType.Flat,
                    acMod,
                    source: source,
                    category: ModifierCategory.Equipment,
                    displayNameOverride: $"Size: {size}"
                ));
            }
            
            // Mobility Modifier (routes to Chassis, used in saves)
            // Large vehicles fail dodges/maneuvers more often
            int mobilityMod = size switch
            {
                VehicleSizeCategory.Tiny => 3,
                VehicleSizeCategory.Small => 1,
                VehicleSizeCategory.Medium => 0,
                VehicleSizeCategory.Large => -3,
                VehicleSizeCategory.Huge => -5,
                _ => 0
            };
            
            if (mobilityMod != 0)
            {
                modifiers.Add(new AttributeModifier(
                    Attribute.Mobility,
                    ModifierType.Flat,
                    mobilityMod,
                    source: source,
                    category: ModifierCategory.Equipment,
                    displayNameOverride: $"Size: {size}"
                ));
            }
            return modifiers;
        }
    }
}
