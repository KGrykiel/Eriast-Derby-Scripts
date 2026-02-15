using System.Collections.Generic;
using Assets.Scripts.Characters;

namespace Assets.Scripts.Combat.SkillChecks
{
    /// <summary>
    /// Rules engine for skill checks — gathers bonuses, rolls via D20Calculator.
    /// Has no vehicle/routing knowledge. Takes pre-resolved participants from CheckRouter.
    /// </summary>
    public static class SkillCheckCalculator
    {
        public static SkillCheckResult Compute(
            SkillCheckSpec checkSpec,
            int dc,
            Entity entity = null,
            Character character = null)
        {
            var bonuses = GatherBonuses(checkSpec, entity, character);
            var roll = D20Calculator.Roll(bonuses, dc);
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
                D20RollHelpers.GatherComponentBonuses(entity, checkSpec.vehicleAttribute, label, bonuses);
            }

            if (character != null && checkSpec.IsCharacterCheck)
            {
                GatherCharacterBonuses(character, checkSpec.characterSkill, bonuses);
            }

            return bonuses;
        }

        // ==================== CHARACTER BONUSES ====================

        private static void GatherCharacterBonuses(
            Character character,
            CharacterSkill skill,
            List<RollBonus> bonuses)
        {
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
        }

        /// <summary>For generating auto-failed skill checks when e.g. no suitable character found</summary>
        public static SkillCheckResult AutoFail(SkillCheckSpec spec, int dc)
        {
            return new SkillCheckResult(
                D20Calculator.AutoFail(dc),
                spec,
                character: null,
                isAutoFail: true);
        }
    }
}

