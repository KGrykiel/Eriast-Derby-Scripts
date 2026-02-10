using System.Collections.Generic;
using Assets.Scripts.Core;
using Assets.Scripts.Entities.Vehicle;

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
        /// <param name="component">Component providing the check bonus</param>
        /// <param name="checkAttribute">Check attribute (Mobility, Stability) - constrained subset for type safety</param>
        /// <param name="displayLabel">Label for the roll bonus tooltip</param>
        /// <param name="bonuses">List to add bonuses to</param>
        public static void GatherComponentBonuses(
            Entity component,
            VehicleCheckAttribute checkAttribute,
            string displayLabel,
            List<RollBonus> bonuses)
        {
            int baseValue = GetComponentBaseValue(component, checkAttribute);
            if (baseValue != 0)
            {
                bonuses.Add(new RollBonus(displayLabel, baseValue));
            }
            
            // Convert to full Attribute only for StatCalculator lookup
            Attribute attribute = checkAttribute.ToAttribute();
            bonuses.AddRange(GatherAppliedBonuses(component, attribute));
        }
        
        /// <summary>
        /// Get the base value a component provides for a d20 check/save.
        /// Delegates to the component itself via polymorphism - components know their own base check values.
        /// Uses VehicleCheckAttribute (constrained subset) for type safety.
        /// </summary>
        private static int GetComponentBaseValue(Entity component, VehicleCheckAttribute checkAttribute)
        {
            if (component == null)
            {
                return 0;
            }
            
            return component.GetBaseCheckValue(checkAttribute);
        }
    }
}

