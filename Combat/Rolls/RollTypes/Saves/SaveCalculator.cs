using System.Collections.Generic;
using Assets.Scripts.Characters;
using Assets.Scripts.Combat.Rolls.Advantage;
using Assets.Scripts.Combat.Rolls.RollSpecs.SpecTypes;

namespace Assets.Scripts.Combat.Rolls.RollTypes.Saves
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
            Entity entity = null,
            Character character = null)
        {
            var bonuses = GatherBonuses(saveSpec, entity, character);
            var grantedSource = saveSpec.grantedMode != RollMode.Normal
                ? new AdvantageSource(saveSpec.DisplayName, saveSpec.grantedMode)
                : default;
            var advantageSources = D20RollHelpers.GatherAdvantageSources(entity, saveSpec, grantedSource);
            var roll = D20Calculator.Roll(bonuses, saveSpec.dc, advantageSources);
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
                bonuses.AddRange(D20RollHelpers.GatherComponentBonuses(entity, saveSpec.vehicleAttribute, label));
            }

            if (character != null && saveSpec.IsCharacterSave)
            {
                bonuses.AddRange(GatherCharacterSaveBonuses(character, saveSpec.characterAttribute));
            }

            return bonuses;
        }

        // ==================== CHARACTER BONUSES ====================

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

        // ==================== HELPERS ====================

        /// <summary>Factory for making auto-failed results if no suitablke component/character found</summary>
        public static SaveResult AutoFail(SaveSpec spec)
        {
            return new SaveResult(
                D20Calculator.AutoFail(spec.dc),
                spec,
                character: null,
                isAutoFail: true);
        }
    }
}
