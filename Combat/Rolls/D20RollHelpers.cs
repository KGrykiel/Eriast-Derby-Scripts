using System.Collections.Generic;
using Assets.Scripts.Combat.Rolls.Advantage;
using Assets.Scripts.Combat.Rolls.RollSpecs;
using Assets.Scripts.Combat.Rolls.RollSpecs.SpecTypes;
using Assets.Scripts.Core;
using Assets.Scripts.Entities.Vehicle;

namespace Assets.Scripts.Combat.Rolls
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

        public static List<RollBonus> GatherComponentBonuses(
            Entity component,
            VehicleCheckAttribute checkAttribute,
            string displayLabel)
        {
            var bonuses = new List<RollBonus>();

            // Convert to full Attribute only for StatCalculator lookup
            Attribute attribute = checkAttribute.ToAttribute();

            int baseValue = component.GetBaseValue(attribute);
            if (baseValue != 0)
            {
                bonuses.Add(new RollBonus(displayLabel, baseValue));
            }

            bonuses.AddRange(GatherAppliedBonuses(component, attribute));
            return bonuses;
        }

        public static List<RollBonus> GatherWeaponBonuses(WeaponComponent weapon)
        {
            var bonuses = new List<RollBonus>();
            if (weapon == null) return bonuses;

            int baseAttackBonus = weapon.GetBaseAttackBonus();
            if (baseAttackBonus != 0)
            {
                bonuses.Add(new RollBonus(weapon.name ?? "Weapon", baseAttackBonus));
            }

            bonuses.AddRange(GatherAppliedBonuses(weapon, Attribute.AttackBonus));
            return bonuses;
        }

        // ==================== ADVANTAGE / DISADVANTAGE ====================

        /// <summary>
        /// Gathers all advantage sources relevant to this roll. Mode resolution happens in D20Calculator.
        /// </summary>
        public static AdvantageSource[] GatherAdvantageSources(
            Entity entity, IRollSpec spec, AdvantageSource grantedSource = default)
        {
            var sources = new List<AdvantageSource>();

            if (grantedSource.Type != RollMode.Normal)
                sources.Add(grantedSource);

            if (entity != null)
                sources.AddRange(GatherEntityAdvantageSources(entity, spec));

            return sources.ToArray();
        }

        private static List<AdvantageSource> GatherEntityAdvantageSources(Entity entity, IRollSpec spec)
        {
            var sources = new List<AdvantageSource>();
            foreach (var applied in entity.GetActiveStatusEffects())
            {
                foreach (var grant in applied.template.advantageGrants)
                {
                    if (GrantMatchesSpec(grant, spec))
                    {
                        string label = !string.IsNullOrEmpty(grant.label)
                            ? grant.label : applied.template.effectName;
                        sources.Add(new AdvantageSource(label, grant.type));
                    }
                }
            }

            Vehicle vehicle = entity is VehicleComponent comp ? comp.ParentVehicle : null;
            if (vehicle == null) return sources;

            foreach (var component in vehicle.AllComponents)
            {
                if (!component.isDestroyed)
                {
                    foreach (var grant in component.advantageGrants)
                    {
                        if (GrantMatchesSpec(grant, spec))
                        {
                            string label = !string.IsNullOrEmpty(grant.label)
                                ? grant.label : component.name;
                            sources.Add(new AdvantageSource(label, grant.type));
                        }
                    }
                }
            }

            return sources;
        }

        private static bool GrantMatchesSpec(AdvantageGrant grant, IRollSpec spec)
        {
            if (grant.targets == null) return false;

            foreach (var target in grant.targets)
            {
                if (spec is SkillCheckSpec check)
                {
                    if (check.IsVehicleCheck && target is VehicleCheckAdvantage vca
                        && (vca.limitTo == null || vca.limitTo.Count == 0
                            || vca.limitTo.Contains(check.vehicleAttribute)))
                        return true;
                    if (check.IsCharacterCheck && target is CharacterCheckAdvantage cca
                        && (cca.limitTo == null || cca.limitTo.Count == 0
                            || cca.limitTo.Contains(check.characterSkill)))
                        return true;
                }
                else if (spec is SaveSpec save)
                {
                    if (save.IsVehicleSave && target is VehicleSaveAdvantage vsa
                        && (vsa.limitTo == null || vsa.limitTo.Count == 0
                            || vsa.limitTo.Contains(save.vehicleAttribute)))
                        return true;
                    if (save.IsCharacterSave && target is CharacterSaveAdvantage csa
                        && (csa.limitTo == null || csa.limitTo.Count == 0
                            || csa.limitTo.Contains(save.characterAttribute)))
                        return true;
                }
                else if (spec is AttackSpec && target is AttackAdvantage)
                {
                    return true;
                }
            }

            return false;
        }

        public static RollMode ResolveMode(AdvantageSource[] sources)
        {
            if (sources == null || sources.Length == 0) return RollMode.Normal;

            bool hasAdvantage = false;
            bool hasDisadvantage = false;
            foreach (var src in sources)
            {
                if (src.Type == RollMode.Advantage) hasAdvantage = true;
                else if (src.Type == RollMode.Disadvantage) hasDisadvantage = true;
            }

            if (hasAdvantage && !hasDisadvantage) return RollMode.Advantage;
            if (hasDisadvantage && !hasAdvantage) return RollMode.Disadvantage;
            return RollMode.Normal;
        }
    }
}

