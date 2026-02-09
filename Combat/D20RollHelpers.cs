using System.Collections.Generic;
using Assets.Scripts.Core;

namespace Assets.Scripts.Combat
{
    /// <summary>
    /// Shared helper methods for d20 roll calculators.
    /// Converts entity modifiers (AttributeModifier) to roll contributions (RollBonus).
    /// </summary>
    public static class D20RollHelpers
    {
        /// <summary>
        /// Gather applied modifiers (status effects, equipment) from an entity
        /// and convert them to RollBonus entries for a d20 roll result.
        /// </summary>
        public static List<RollBonus> GatherAppliedBonuses(Entity entity, Attribute attribute)
        {
            var bonuses = new List<RollBonus>();
            if (entity == null) return bonuses;
            
            var (_, _, appliedMods) = StatCalculator.GatherAttributeValueWithBreakdown(
                entity, attribute, 0);
            
            foreach (var mod in appliedMods)
            {
                if (mod.Value != 0)
                {
                    bonuses.Add(new RollBonus(mod.SourceDisplayName, (int)mod.Value));
                }
            }
            
            return bonuses;
        }
        
        /// <summary>
        /// Sum all bonus values in a list. Shared by all calculators.
        /// </summary>
        public static int SumBonuses(List<RollBonus> bonuses)
        {
            int sum = 0;
            foreach (var b in bonuses) sum += b.Value;
            return sum;
        }
        
        /// <summary>
        /// Gather component bonuses for a vehicle attribute check/save.
        /// Gets the base value from the component and any applied modifiers.
        /// </summary>
        public static void GatherComponentBonuses(
            Entity component,
            Attribute vehicleAttribute,
            string displayLabel,
            List<RollBonus> bonuses)
        {
            int baseValue = GetComponentBaseValue(component, vehicleAttribute);
            if (baseValue != 0)
            {
                bonuses.Add(new RollBonus(displayLabel, baseValue));
            }
            
            bonuses.AddRange(GatherAppliedBonuses(component, vehicleAttribute));
        }
        
        /// <summary>
        /// Get the base value a component provides for a vehicle attribute.
        /// </summary>
        private static int GetComponentBaseValue(Entity component, Attribute vehicleAttribute)
        {
            if (component is ChassisComponent chassis)
            {
                return vehicleAttribute switch
                {
                    Attribute.Mobility => chassis.GetBaseMobility(),
                    _ => 0
                };
            }
            return 0;
        }
    }
}

