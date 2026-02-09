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
    }
}

