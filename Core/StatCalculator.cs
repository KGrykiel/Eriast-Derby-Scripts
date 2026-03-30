using System.Collections.Generic;
using Assets.Scripts.Entities;
using Assets.Scripts.Modifiers;
using Assets.Scripts.Modifiers.DynamicModifiers;

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
        public static (int total, int baseValue, List<EntityAttributeModifier> modifiers) GatherAttributeValueWithBreakdown(
            Entity entity,
            EntityAttribute attribute)
        {
            int baseValue = entity.GetBaseValue(attribute);
            var entityModifiers = GatherEntityModifiers(entity, attribute);
            var dynamicModifiers = DynamicModifierEvaluator.EvaluateAll(entity, attribute);

            var allModifiers = new List<EntityAttributeModifier>(entityModifiers.Count + dynamicModifiers.Count);
            allModifiers.AddRange(entityModifiers);
            allModifiers.AddRange(dynamicModifiers);

            int total = ModifierCalculator.CalculateTotal(baseValue, allModifiers);

            return (total, baseValue, allModifiers);
        }

        /// <summary>Convenience overload when you don't need the breakdown.</summary>
        public static int GatherAttributeValue(Entity entity, EntityAttribute attribute)
        {
            var (total, _, _) = GatherAttributeValueWithBreakdown(entity, attribute);
            return total;
        }

        /// <summary>Convenience method to get AC</summary>
        public static (int total, int baseValue, List<EntityAttributeModifier> modifiers) GatherDefenseValueWithBreakdown(
            Entity target)
        {
            return GatherAttributeValueWithBreakdown(target, EntityAttribute.ArmorClass);
        }

        // ==================== PRIVATE ====================

        /// <summary>
        /// Gather matching modifiers from an entity's modifier list.
        /// Returns a new list — does not mutate the entity.
        /// </summary>
        private static List<EntityAttributeModifier> GatherEntityModifiers(Entity entity, EntityAttribute attribute)
        {
            var modifiers = new List<EntityAttributeModifier>();

            foreach (var mod in entity.GetModifiers())
            {
                if (mod.Attribute == attribute && mod.Value != 0)
                {
                    modifiers.Add(mod);
                }
            }

            return modifiers;
        }
    }
}
