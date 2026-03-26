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

        public static GatheredRoll ForSkillCheck(SkillCheckSpec spec, RollActor actor)
        {
            var bonuses = new List<RollBonus>();
            Entity entity = GetContextEntity(actor);
            VehicleSeat seat = null;

            if (spec is VehicleSkillCheckSpec vehicleSpec && actor is ComponentActor componentActor)
            {
                string label = componentActor.Component.name ?? vehicleSpec.DisplayName;
                bonuses.AddRange(GatherComponentBonuses(componentActor.Component, vehicleSpec.vehicleAttribute, label));
            }
            else if (spec is CharacterSkillCheckSpec charSpec)
            {
                seat = actor.GetSeat();
                if (seat != null)
                    bonuses.AddRange(GatherCharacterSkillBonuses(seat, charSpec.characterSkill));
            }

            var grantedSource = ResolveGrantedSource(spec.grantedMode, spec.DisplayName);
            var advantageSources = GatherAdvantageSources(entity, seat, spec, grantedSource);

            return new GatheredRoll(bonuses, advantageSources);
        }

        private static List<RollBonus> GatherCharacterSkillBonuses(
            VehicleSeat seat,
            CharacterSkill skill)
        {
            var bonuses = new List<RollBonus>();
            var (_, attrBonus, profBonus, directMods) = CharacterStatCalculator.GatherSkillBonusWithBreakdown(seat, skill);

            CharacterAttribute attribute = CharacterSkillHelper.GetPrimaryAttribute(skill);
            if (attrBonus != 0)
                bonuses.Add(new RollBonus($"{attribute} Modifier", attrBonus));

            if (profBonus != 0)
                bonuses.Add(new RollBonus("Proficiency", profBonus));

            foreach (var mod in directMods)
                if (mod.Value != 0)
                    bonuses.Add(new RollBonus(mod.Label, (int)mod.Value));

            return bonuses;
        }

        // ==================== SAVES ====================

        public static GatheredRoll ForSave(SaveSpec spec, RollActor actor)
        {
            var bonuses = new List<RollBonus>();
            Entity entity = GetContextEntity(actor);
            VehicleSeat seat = null;

            if (spec is VehicleSaveSpec vehicleSpec && actor is ComponentActor componentActor)
            {
                string label = componentActor.Component.name ?? vehicleSpec.DisplayName;
                bonuses.AddRange(GatherComponentBonuses(componentActor.Component, vehicleSpec.vehicleAttribute, label));
            }
            else if (spec is CharacterSaveSpec charSpec)
            {
                seat = actor.GetSeat();
                if (seat != null)
                    bonuses.AddRange(GatherCharacterSaveBonuses(seat, charSpec.characterAttribute));
            }

            var grantedSource = ResolveGrantedSource(spec.grantedMode, spec.DisplayName);
            var advantageSources = GatherAdvantageSources(entity, seat, spec, grantedSource);

            return new GatheredRoll(bonuses, advantageSources);
        }

        private static List<RollBonus> GatherCharacterSaveBonuses(
            VehicleSeat seat,
            CharacterAttribute attribute)
        {
            var bonuses = new List<RollBonus>();
            var (_, attrBonus, levelBonus) = CharacterStatCalculator.GatherSaveBonusWithBreakdown(seat, attribute);

            if (attrBonus != 0)
                bonuses.Add(new RollBonus($"{attribute} Modifier", attrBonus));

            if (levelBonus != 0)
                bonuses.Add(new RollBonus("Half Level", levelBonus));

            return bonuses;
        }

        // ==================== ATTACKS ====================

        public static GatheredRoll ForAttack(AttackSpec spec, RollActor actor)
        {
            var bonuses = new List<RollBonus>();
            Entity entity = GetContextEntity(actor);

            if (entity is WeaponComponent weapon)
            {
                bonuses.AddRange(GatherWeaponBonuses(weapon));
            }

            VehicleSeat seat = actor.GetSeat();
            if (seat != null && seat.IsAssigned)
            {
                int charBonus = CharacterStatCalculator.CalculateAttackBonus(seat.GetBaseAttackBonus());
                if (charBonus != 0)
                {
                    bonuses.Add(new RollBonus(seat.GetDisplayName(), charBonus));
                }
            }

            var grantedSource = ResolveGrantedSource(spec.grantedMode, "Attack");
            var advantageSources = GatherAdvantageSources(entity, seat, spec, grantedSource);

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

            var advantageSources = GatherAdvantageSources(entity, null, spec, default);

            return new GatheredRoll(bonuses, advantageSources);
        }

        // ==================== GATHERING HELPERS ====================

        private static Entity GetContextEntity(RollActor actor)
        {
            return actor.GetEntity();
        }

        private static AdvantageSource ResolveGrantedSource(RollMode grantedMode, string label)
        {
            return grantedMode != RollMode.Normal
                ? new AdvantageSource(label, grantedMode)
                : default;
        }

        public static List<AdvantageSource> GatherAdvantageSources(
            Entity entity, VehicleSeat seat, IRollSpec spec, AdvantageSource grantedSource)
        {
            var sources = new List<AdvantageSource>();

            if (grantedSource.Type != RollMode.Normal)
                sources.Add(grantedSource);

            if (entity != null)
                sources.AddRange(GatherEntityAdvantageSources(entity, spec));

            if (seat != null)
                sources.AddRange(GatherSeatConditionAdvantages(seat, spec));

            return sources;
        }

        private static List<AdvantageSource> GatherSeatConditionAdvantages(VehicleSeat seat, IRollSpec spec)
        {
            var sources = new List<AdvantageSource>();

            foreach (var applied in seat.GetActiveConditions())
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

            return sources;
        }

        private static List<AdvantageSource> GatherEntityAdvantageSources(Entity entity, IRollSpec spec)
        {
            var sources = new List<AdvantageSource>();
            foreach (var applied in entity.GetActiveConditions())
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
                if (!component.IsDestroyed())
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
                if (spec is VehicleSkillCheckSpec vCheck)
                {
                    if (target is VehicleCheckAdvantage vca
                        && (vca.limitTo == null || vca.limitTo.Count == 0
                            || vca.limitTo.Contains(vCheck.vehicleAttribute)))
                        return true;
                }
                else if (spec is CharacterSkillCheckSpec cCheck)
                {
                    if (target is CharacterCheckAdvantage cca
                        && (cca.limitTo == null || cca.limitTo.Count == 0
                            || cca.limitTo.Contains(cCheck.characterSkill)))
                        return true;
                }
                else if (spec is VehicleSaveSpec vSave)
                {
                    if (target is VehicleSaveAdvantage vsa
                        && (vsa.limitTo == null || vsa.limitTo.Count == 0
                            || vsa.limitTo.Contains(vSave.vehicleAttribute)))
                        return true;
                }
                else if (spec is CharacterSaveSpec cSave)
                {
                    if (target is CharacterSaveAdvantage csa
                        && (csa.limitTo == null || csa.limitTo.Count == 0
                            || csa.limitTo.Contains(cSave.characterAttribute)))
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

            var (_, _, appliedMods) = StatCalculator.GatherAttributeValueWithBreakdown(entity, attribute);

            foreach (var mod in appliedMods)
            {
                if (mod.Value != 0)
                {
                    bonuses.Add(new RollBonus(mod.Label, (int)mod.Value));
                }
            }

            return bonuses;
        }
    }
}
