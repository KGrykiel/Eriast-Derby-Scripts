using System.Collections.Generic;
using Assets.Scripts.Characters;

namespace Assets.Scripts.Combat.SkillChecks
{
    /// <summary>
    /// Calculator for skill checks (d20 + bonuses vs DC).
    /// Primary entry point takes a vehicle — resolution is handled internally.
    /// Returns null if the check can't be attempted (destroyed component, no character, etc.).
    /// </summary>
    public static class SkillCheckCalculator
    {
        /// <summary>
        /// Perform a skill check for a vehicle. Resolves which component and character
        /// are involved, gathers bonuses, rolls, returns complete result.
        /// </summary>
        /// <param name="initiatingCharacter">Character who initiated this check (for character-initiated skills). 
        /// Null for vehicle-wide checks like event cards, which route to best character.</param>
        public static SkillCheckResult PerformSkillCheck(
            Vehicle vehicle,
            CheckSpec checkSpec,
            int dc,
            Character initiatingCharacter = null)
        {
            var routing = CheckRouter.RouteSkillCheck(vehicle, checkSpec, initiatingCharacter);
            if (!routing.CanAttempt)
            {
                // Component required but unavailable - automatic failure
                return SkillCheckResult.AutoFail(checkSpec, dc);
            }

            return PerformSkillCheck(checkSpec, dc, routing.Component, routing.Character);
        }

        /// <summary>
        /// Perform a skill check with already-routed component and character.
        /// Internal helper - use the vehicle-level overload instead.
        /// </summary>
        private static SkillCheckResult PerformSkillCheck(
            CheckSpec checkSpec,
            int dc,
            Entity component = null,
            Character character = null)
        {
            int baseRoll = RollUtility.RollD20();
            var bonuses = GatherBonuses(checkSpec, component, character);
            int total = baseRoll + D20RollHelpers.SumBonuses(bonuses);
            bool success = total >= dc;
            
            return new SkillCheckResult(baseRoll, checkSpec, bonuses, dc, success, character);
        }
        
        /// <summary>
        /// Gather all bonuses for a skill check.
        /// Component provides: base value + applied modifiers (status effects, equipment).
        /// Character provides: attribute modifier + proficiency bonus.
        /// </summary>
        public static List<RollBonus> GatherBonuses(
            CheckSpec checkSpec,
            Entity component = null,
            Character character = null)
        {
            var bonuses = new List<RollBonus>();
            
            // Component bonuses (vehicle attribute checks)
            if (component != null && checkSpec.IsVehicleCheck)
            {
                string label = component.name ?? checkSpec.DisplayName;
                D20RollHelpers.GatherComponentBonuses(component, checkSpec.vehicleAttribute, label, bonuses);
            }
            
            // Character bonuses (attribute modifier + proficiency)
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
            
            int attrMod = character.GetAttributeModifier(attribute);
            if (attrMod != 0)
            {
                bonuses.Add(new RollBonus($"{attribute} Modifier", attrMod));
            }
            
            int proficiency = character.GetProficiencyBonus(skill);
            if (proficiency != 0)
            {
                bonuses.Add(new RollBonus("Proficiency", proficiency));
            }
        }
    }
}

