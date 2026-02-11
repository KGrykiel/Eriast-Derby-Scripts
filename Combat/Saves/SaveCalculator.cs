using System.Collections.Generic;
using Assets.Scripts.Characters;

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
        /// Perform a saving throw for a vehicle. Resolves who/what saves internally via CheckRouter.
        /// Use this for vehicle targets where component/character resolution is needed.
        /// </summary>
        public static SaveResult PerformSavingThrow(
            Vehicle vehicle,
            SaveSpec saveSpec,
            int dc,
            VehicleComponent targetComponent = null)
        {
            var routing = CheckRouter.RouteSave(vehicle, saveSpec, targetComponent);
            if (!routing.CanAttempt)
            {
                // Component required but unavailable - automatic failure
                return SaveResult.AutoFail(saveSpec, dc);
            }

            return PerformSavingThrowInternal(saveSpec, dc, routing.Component, routing.Character);
        }

        /// <summary>
        /// Perform a saving throw for a standalone entity (non-vehicle target).
        /// Use this for entities that don't require CheckRouter resolution (golems, turrets, etc.).
        /// For vehicle targets, use the Vehicle overload instead.
        /// </summary>
        public static SaveResult PerformSavingThrowForEntity(
            SaveSpec saveSpec,
            int dc,
            Entity entity,
            Character character = null)
        {
            return PerformSavingThrowInternal(saveSpec, dc, entity, character);
        }

        /// <summary>
        /// Internal implementation - performs the actual save calculation.
        /// Callers should use the appropriate public entry point.
        /// </summary>
        private static SaveResult PerformSavingThrowInternal(
            SaveSpec saveSpec,
            int dc,
            Entity component = null,
            Character character = null)
        {
            int baseRoll = RollUtility.RollD20();
            var bonuses = GatherBonuses(saveSpec, component, character);
            int total = baseRoll + D20RollHelpers.SumBonuses(bonuses);
            bool success = total >= dc;
            
            return new SaveResult(baseRoll, saveSpec, bonuses, dc, success, character);
        }
        
        /// <summary>
        /// Gather all bonuses for a saving throw.
        /// Vehicle saves: component base value + applied modifiers.
        /// Character saves: attribute modifier + half level.
        /// </summary>
        public static List<RollBonus> GatherBonuses(
            SaveSpec saveSpec,
            Entity component = null,
            Character character = null)
        {
            var bonuses = new List<RollBonus>();
            
            // Component bonuses (vehicle saves)
            if (component != null && saveSpec.IsVehicleSave)
            {
                string label = component.name ?? saveSpec.DisplayName;
                D20RollHelpers.GatherComponentBonuses(component, saveSpec.vehicleAttribute, label, bonuses);
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
            Character character,
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
