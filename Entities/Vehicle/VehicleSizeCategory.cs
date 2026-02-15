using System.Collections.Generic;

namespace Assets.Scripts.Entities.Vehicle
{
    /// <summary>
    /// Bonuses/Penalties for different vehicle sizes to level the playing field and keep realism.
    /// Otherwise larger vehicles would have an explicit advantage in all aspects of the race.
    /// </summary>
    public enum VehicleSizeCategory
    {
        /// <summary> +3 AC, +3 Mobility, Nimble.</summary>
        Tiny,
        
        /// <summary> +1 AC, +1 Mobility, Nimble.</summary>
        Small,
        
        /// <summary>Baseline (0 modifiers).</summary>
        Medium,
        
        /// <summary> -2 AC, -3 Mobility, Slow.</summary>
        Large,
        
        /// <summary> -4 AC, -5 Mobility, Very Slow.</summary>
        Huge
    }
    
    public static class VehicleSizeModifiers
    {
        public static List<AttributeModifier> GetModifiers(VehicleSizeCategory size, UnityEngine.Object source)
        {
            var modifiers = new List<AttributeModifier>();
            
            // AC Modifier for chassis
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

            // add more modifiers as needed, maybe acceleration or maxSpeed.
        }
    }
}
