using System.Collections.Generic;
using Assets.Scripts.Core;
using Assets.Scripts.Entities.Vehicle;

namespace Assets.Scripts.Combat
{
    /// <summary>Common classes used across Saves and skillcheks extracted to avoid repetition</summary>
    public static class D20RollHelpers
    {
        private static List<RollBonus> GatherAppliedBonuses(Entity entity, Attribute attribute)
        {
            var bonuses = new List<RollBonus>();
            if (entity == null) return bonuses;

            var (_, _, appliedMods) = StatCalculator.GatherAttributeValueWithBreakdown(
                entity, attribute);

            foreach (var mod in appliedMods)
            {
                if (mod.Value != 0)
                {
                    bonuses.Add(new RollBonus(mod.SourceDisplayName, (int)mod.Value));
                }
            }

            return bonuses;
        }

        public static int SumBonuses(List<RollBonus> bonuses)
        {
            int sum = 0;
            foreach (var b in bonuses) sum += b.Value;
            return sum;
        }

        public static void GatherComponentBonuses(
            Entity component,
            VehicleCheckAttribute checkAttribute,
            string displayLabel,
            List<RollBonus> bonuses)
        {
            // Convert to full Attribute only for StatCalculator lookup
            Attribute attribute = checkAttribute.ToAttribute();

            int baseValue = component.GetBaseValue(attribute);
            if (baseValue != 0)
            {
                bonuses.Add(new RollBonus(displayLabel, baseValue));
            }

            bonuses.AddRange(GatherAppliedBonuses(component, attribute));
        }

        public static void GatherWeaponBonuses(
            WeaponComponent weapon,
            List<RollBonus> bonuses)
        {
            if (weapon == null) return;

            int baseAttackBonus = weapon.GetBaseAttackBonus();
            if (baseAttackBonus != 0)
            {
                bonuses.Add(new RollBonus(weapon.name ?? "Weapon", baseAttackBonus));
            }

            bonuses.AddRange(GatherAppliedBonuses(weapon, Attribute.AttackBonus));
        }
    }
}

