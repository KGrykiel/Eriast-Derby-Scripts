using System.Collections.Generic;
using Assets.Scripts.Characters;

namespace Assets.Scripts.Combat.SkillChecks
{
    /// <summary>
    /// Rules engine for skill checks.
    /// Gathers bonuses according to game rules, rolls via D20Calculator, wraps in result.
    /// 
    /// UNIVERSAL: No vehicle, component, or routing knowledge.
    /// Takes pre-resolved participants (entity for vehicle checks, character for character checks).
    /// 
    /// Vehicle routing happens EXTERNALLY via CheckRouter before calling this.
    /// Non-vehicle entities call this directly.
    /// </summary>
    public static class SkillCheckCalculator
    {
        /// <summary>
        /// Compute a skill check with pre-resolved participants.
        /// Gathers bonuses from the given entity/character based on spec, rolls, returns result.
        /// 
        /// For vehicle checks: pass the resolved component as entity.
        /// For character checks: pass the resolved character.
        /// For standalone entities: pass the entity directly.
        /// </summary>
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

        /// <summary>
        /// Gather all bonuses for a skill check based on game rules.
        /// Entity provides: base value + applied modifiers (status effects, equipment).
        /// Character provides: attribute modifier + proficiency bonus.
        /// </summary>
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

        /// <summary>
        /// Create an automatic failure result (routing failed - no roll occurred).
        /// </summary>
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

