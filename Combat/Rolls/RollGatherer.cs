using System.Collections.Generic;
using Assets.Scripts.Characters;
using Assets.Scripts.Combat.Rolls.Advantage;
using Assets.Scripts.Combat.Rolls.RollSpecs;
using Assets.Scripts.Combat.Rolls.RollSpecs.SpecTypes;
using Assets.Scripts.Core;
using Assets.Scripts.Entities.Vehicle;

namespace Assets.Scripts.Combat.Rolls
{
    /// <summary>
    /// Centralized gathering of all bonuses and advantages for d20 rolls.
    /// Single place for future character condition logic (Phase 2b).
    /// </summary>
    public static class RollGatherer
    {
        // ==================== SKILL CHECKS ====================

        public static GatheredRoll ForSkillCheck(
            SkillCheckSpec spec,
            Entity entity = null,
            Character character = null)
        {
            var bonuses = new List<RollBonus>();

            if (entity != null && spec.IsVehicleCheck)
            {
                string label = entity.name ?? spec.DisplayName;
                bonuses.AddRange(GatherComponentBonuses(entity, spec.vehicleAttribute, label));
            }

            if (character != null && spec.IsCharacterCheck)
            {
                bonuses.AddRange(GatherCharacterSkillBonuses(character, spec.characterSkill));
            }

            var grantedSource = ResolveGrantedSource(spec.grantedMode, spec.DisplayName);
            var advantageSources = GatherAdvantageSources(entity, spec, grantedSource);

            return new GatheredRoll(bonuses, advantageSources);
        }

        private static List<RollBonus> GatherCharacterSkillBonuses(
            Character character,
            CharacterSkill skill)
        {
            var bonuses = new List<RollBonus>();

            CharacterAttribute attribute = CharacterSkillHelper.GetPrimaryAttribute(skill);

            int attributeScore = character.GetAttributeScore(attribute);
            int attrMod = CharacterFormulas.CalculateAttributeModifier(attributeScore);
            if (attrMod != 0)
            {
                bonuses.Add(new RollBonus($"{attribute} Modifier", attrMod));
            }

            if (character.IsProficient(skill))
            {
                int proficiency = CharacterFormulas.CalculateProficiencyBonus(character.level);
                if (proficiency != 0)
                {
                    bonuses.Add(new RollBonus("Proficiency", proficiency));
                }
            }

            return bonuses;
        }

        // ==================== SAVES ====================

        public static GatheredRoll ForSave(
            SaveSpec spec,
            Entity entity = null,
            Character character = null)
        {
            var bonuses = new List<RollBonus>();

            if (entity != null && spec.IsVehicleSave)
            {
                string label = entity.name ?? spec.DisplayName;
                bonuses.AddRange(GatherComponentBonuses(entity, spec.vehicleAttribute, label));
            }

            if (character != null && spec.IsCharacterSave)
            {
                bonuses.AddRange(GatherCharacterSaveBonuses(character, spec.characterAttribute));
            }

            var grantedSource = ResolveGrantedSource(spec.grantedMode, spec.DisplayName);
            var advantageSources = GatherAdvantageSources(entity, spec, grantedSource);

            return new GatheredRoll(bonuses, advantageSources);
        }

        private static List<RollBonus> GatherCharacterSaveBonuses(
            Character character,
            CharacterAttribute attribute)
        {
            var bonuses = new List<RollBonus>();

            int attributeScore = character.GetAttributeScore(attribute);
            int attrMod = CharacterFormulas.CalculateAttributeModifier(attributeScore);
            if (attrMod != 0)
            {
                bonuses.Add(new RollBonus($"{attribute} Modifier", attrMod));
            }

            int halfLevel = CharacterFormulas.CalculateHalfLevelBonus(character.level);
            if (halfLevel != 0)
            {
                bonuses.Add(new RollBonus("Half Level", halfLevel));
            }

            return bonuses;
        }

        // ==================== ATTACKS ====================

        public static GatheredRoll ForAttack(
            AttackSpec spec,
            Entity attacker = null,
            Character character = null)
        {
            var bonuses = new List<RollBonus>();

            if (attacker is WeaponComponent weapon)
            {
                bonuses.AddRange(GatherWeaponBonuses(weapon));
            }

            if (character != null)
            {
                int charBonus = CharacterFormulas.CalculateAttackBonus(character);
                if (charBonus != 0)
                {
                    bonuses.Add(new RollBonus(character.characterName, charBonus));
                }
            }

            var grantedSource = ResolveGrantedSource(spec.grantedMode, "Attack");
            var advantageSources = GatherAdvantageSources(attacker, spec, grantedSource);

            return new GatheredRoll(bonuses, advantageSources);
        }

        // ==================== ENTITY-ONLY (OPPOSED CHECKS) ====================

        /// <summary>
        /// Gather roll data for entity-only checks (e.g., opposed pursuit vs. chassis).
        /// Used when there's no character involvement, just pure entity attributes.
        /// </summary>
        public static GatheredRoll ForEntity(
            IRollSpec spec,
            Entity entity,
            VehicleCheckAttribute attribute)
        {
            var bonuses = new List<RollBonus>();

            if (entity != null)
            {
                string label = entity.name ?? "Entity";
                bonuses.AddRange(GatherComponentBonuses(entity, attribute, label));
            }

            var advantageSources = GatherAdvantageSources(entity, spec, default);

            return new GatheredRoll(bonuses, advantageSources);
        }

        // ==================== GATHERING HELPERS ====================

        private static AdvantageSource ResolveGrantedSource(RollMode grantedMode, string label)
        {
            return grantedMode != RollMode.Normal
                ? new AdvantageSource(label, grantedMode)
                : default;
        }

        public static List<AdvantageSource> GatherAdvantageSources(
            Entity entity, IRollSpec spec, AdvantageSource grantedSource = default)
        {
            var sources = new List<AdvantageSource>();

            if (grantedSource.Type != RollMode.Normal)
                sources.Add(grantedSource);

            if (entity != null)
                sources.AddRange(GatherEntityAdvantageSources(entity, spec));

            return sources;
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

        private static List<RollBonus> GatherComponentBonuses(
            Entity component,
            VehicleCheckAttribute checkAttribute,
            string displayLabel)
        {
            var bonuses = new List<RollBonus>();
            Attribute attribute = checkAttribute.ToAttribute();

            int baseValue = component.GetBaseValue(attribute);
            if (baseValue != 0)
            {
                bonuses.Add(new RollBonus(displayLabel, baseValue));
            }

            bonuses.AddRange(GatherAppliedBonuses(component, attribute));
            return bonuses;
        }

        private static List<RollBonus> GatherWeaponBonuses(WeaponComponent weapon)
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

        private static List<RollBonus> GatherAppliedBonuses(Entity entity, Attribute attribute)
        {
            var bonuses = new List<RollBonus>();
            if (entity == null) return bonuses;

            var (_, _, appliedMods) = StatCalculator.GatherAttributeValueWithBreakdown(entity, attribute);

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
