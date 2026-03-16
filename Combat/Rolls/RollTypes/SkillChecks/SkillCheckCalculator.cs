using System.Collections.Generic;
using Assets.Scripts.Characters;
using Assets.Scripts.Combat.Rolls.Advantage;
using Assets.Scripts.Combat.Rolls.RollSpecs.SpecTypes;

namespace Assets.Scripts.Combat.Rolls.RollTypes.SkillChecks
{
    /// <summary>
    /// Rules engine for skill checks — gathers bonuses, rolls via D20Calculator.
    /// Has no vehicle/routing knowledge. Takes pre-resolved participants from CheckRouter.
    /// </summary>
    public static class SkillCheckCalculator
    {
        public static SkillCheckResult Compute(
            SkillCheckSpec checkSpec,
            Entity entity = null,
            Character character = null)
        {
            var bonuses = GatherBonuses(checkSpec, entity, character);
            var grantedSource = checkSpec.grantedMode != RollMode.Normal
                ? new AdvantageSource(checkSpec.DisplayName, checkSpec.grantedMode)
                : default;
            var advantageSources = D20RollHelpers.GatherAdvantageSources(entity, checkSpec, grantedSource);
            var roll = D20Calculator.Roll(bonuses, checkSpec.dc, advantageSources);
            return new SkillCheckResult(roll, checkSpec, character);
        }

        public static List<RollBonus> GatherBonuses(
            SkillCheckSpec checkSpec,
            Entity entity = null,
            Character character = null)
        {
            var bonuses = new List<RollBonus>();

            if (entity != null && checkSpec.IsVehicleCheck)
            {
                string label = entity.name ?? checkSpec.DisplayName;
                bonuses.AddRange(D20RollHelpers.GatherComponentBonuses(entity, checkSpec.vehicleAttribute, label));
            }

            if (character != null && checkSpec.IsCharacterCheck)
            {
                bonuses.AddRange(GatherCharacterBonuses(character, checkSpec.characterSkill));
            }

            return bonuses;
        }

        // ==================== CHARACTER BONUSES ====================

        private static List<RollBonus> GatherCharacterBonuses(
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

        /// <summary>For generating auto-failed skill checks when e.g. no suitable character found</summary>
        public static SkillCheckResult AutoFail(SkillCheckSpec spec)
        {
            return new SkillCheckResult(
                D20Calculator.AutoFail(spec.dc),
                spec,
                character: null,
                isAutoFail: true);
        }
    }
}