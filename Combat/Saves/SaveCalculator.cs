using System.Collections.Generic;
using Assets.Scripts.Characters;

namespace Assets.Scripts.Combat.Saves
{
    /// <summary>
    /// Rules engine for saving throws — gathers bonuses, rolls via D20Calculator.
    /// Has no vehicle/routing knowledge. Takes pre-resolved participants from CheckRouter.
    /// </summary>
    public static class SaveCalculator
    {
        /// <summary>
        /// pass entity if this is a vehicle save or non-vehicle entity save
        /// pass character if it's a character save
        /// </summary>
        public static SaveResult Compute(
            SaveSpec saveSpec,
            int dc,
            Entity entity = null,
            Character character = null)
        {
            var bonuses = GatherBonuses(saveSpec, entity, character);
            var roll = D20Calculator.Roll(bonuses, dc);
            return new SaveResult(roll, saveSpec, character);
        }

        public static List<RollBonus> GatherBonuses(
            SaveSpec saveSpec,
            Entity entity = null,
            Character character = null)
        {
            var bonuses = new List<RollBonus>();

            if (entity != null && saveSpec.IsVehicleSave)
            {
                string label = entity.name ?? saveSpec.DisplayName;
                D20RollHelpers.GatherComponentBonuses(entity, saveSpec.vehicleAttribute, label, bonuses);
            }

            if (character != null && saveSpec.IsCharacterSave)
            {
                GatherCharacterSaveBonuses(character, saveSpec.characterAttribute, bonuses);
            }

            return bonuses;
        }

        // ==================== CHARACTER BONUSES ====================

        private static void GatherCharacterSaveBonuses(
            Character character,
            CharacterAttribute attribute,
            List<RollBonus> bonuses)
        {
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
        }

        // ==================== HELPERS ====================

        public static int CalculateSaveDC(Skill skill)
        {
            return skill.saveDCBase;
        }

        /// <summary>Factory for making auto-failed results if no suitablke component/character found</summary>
        public static SaveResult AutoFail(SaveSpec spec, int dc)
        {
            return new SaveResult(
                D20Calculator.AutoFail(dc),
                spec,
                character: null,
                isAutoFail: true);
        }
    }
}
