using System.Collections.Generic;
using Assets.Scripts.Characters;

namespace Assets.Scripts.Combat.Saves
{
    /// <summary>
    /// Rules engine for saving throws.
    /// Gathers bonuses according to game rules, rolls via D20Calculator, wraps in result.
    /// 
    /// UNIVERSAL: No vehicle, component, or routing knowledge.
    /// Takes pre-resolved participants (entity for vehicle saves, character for character saves).
    /// 
    /// Vehicle routing happens EXTERNALLY via CheckRouter before calling this.
    /// Non-vehicle entities call this directly.
    /// </summary>
    public static class SaveCalculator
    {
        /// <summary>
        /// Compute a saving throw with pre-resolved participants.
        /// Gathers bonuses from the given entity/character based on spec, rolls, returns result.
        /// 
        /// For vehicle saves: pass the resolved component as entity.
        /// For character saves: pass the resolved character.
        /// For standalone entities: pass the entity directly.
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

        /// <summary>
        /// Gather all bonuses for a saving throw based on game rules.
        /// Vehicle saves: entity base value + applied modifiers.
        /// Character saves: attribute modifier + half level.
        /// </summary>
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

        public static int CalculateSaveDC(Skill skill, Entity user)
        {
            return skill.saveDCBase;
        }

        /// <summary>
        /// Create an automatic failure result (routing failed - no roll occurred).
        /// </summary>
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
