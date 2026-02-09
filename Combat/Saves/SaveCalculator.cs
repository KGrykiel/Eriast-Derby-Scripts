using System.Collections.Generic;
using Assets.Scripts.Characters;
using Assets.Scripts.Entities.Vehicle;

namespace Assets.Scripts.Combat.Saves
{
    /// <summary>
    /// Calculator for saving throws (d20 + bonuses vs DC).
    /// Primary entry point takes a vehicle — resolution is handled internally.
    /// Returns null if the save can't be attempted.
    /// </summary>
    public static class SaveCalculator
    {
        /// <summary>
        /// Perform a saving throw for a vehicle. Resolves who/what saves internally.
        /// Returns null if the save can't be attempted.
        /// </summary>
        public static SaveResult PerformSavingThrow(
            Vehicle vehicle,
            SaveSpec saveSpec,
            int dc,
            RoleType preferredRole = RoleType.None,
            VehicleComponent targetComponent = null)
        {
            var resolution = CheckResolver.ResolveSave(vehicle, saveSpec, preferredRole, targetComponent);
            if (!resolution.CanAttempt)
                return null;
            
            return PerformSavingThrow(saveSpec, dc, resolution.Component, resolution.Character);
        }

        /// <summary>
        /// Perform a saving throw with explicit DC and resolved component/character.
        /// </summary>
        public static SaveResult PerformSavingThrow(
            SaveSpec saveSpec,
            int dc,
            Entity component = null,
            PlayerCharacter character = null)
        {
            int baseRoll = RollUtility.RollD20();
            var bonuses = GatherBonuses(saveSpec, component, character);
            int total = baseRoll + D20RollHelpers.SumBonuses(bonuses);
            bool success = total >= dc;
            
            return new SaveResult(baseRoll, saveSpec, bonuses, dc, success);
        }
        
        /// <summary>
        /// Gather all bonuses for a saving throw.
        /// Vehicle saves: component base value + applied modifiers.
        /// Character saves: attribute modifier + half level.
        /// </summary>
        public static List<RollBonus> GatherBonuses(
            SaveSpec saveSpec,
            Entity component = null,
            PlayerCharacter character = null)
        {
            var bonuses = new List<RollBonus>();
            
            // Component bonuses (vehicle saves)
            if (component != null && saveSpec.IsVehicleSave)
            {
                string label = component.name ?? saveSpec.DisplayName;
                D20RollHelpers.GatherComponentBonuses(component, saveSpec.vehicleAttribute.ToAttribute(), label, bonuses);
            }
            
            // Character bonuses (character saves)
            if (character != null && saveSpec.IsCharacterSave)
            {
                GatherCharacterSaveBonuses(character, saveSpec.characterAttribute, bonuses);
            }
            
            return bonuses;
        }
        
        // ==================== CHARACTER BONUSES ====================
        
        /// <summary>
        /// Character save bonus: attribute modifier + half level (rounded down).
        /// </summary>
        private static void GatherCharacterSaveBonuses(
            PlayerCharacter character,
            CharacterAttribute attribute,
            List<RollBonus> bonuses)
        {
            int attrMod = character.GetAttributeModifier(attribute);
            if (attrMod != 0)
            {
                bonuses.Add(new RollBonus($"{attribute} Modifier", attrMod));
            }
            
            int halfLevel = character.level / 2;
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
    }
}
