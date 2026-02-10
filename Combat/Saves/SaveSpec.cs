using UnityEngine;
using Assets.Scripts.Characters;
using Assets.Scripts.Entities.Vehicle;

namespace Assets.Scripts.Combat.Saves
{
    /// <summary>
    /// Describes what a saving throw tests — either a vehicle attribute or a character attribute.
    /// Replaces the flat SaveType enum, eliminating redundant CharacterAttribute mirrors.
    /// 
    /// Vehicle saves: domain=Vehicle, vehicleAttribute=Mobility (tests the vehicle's own stat).
    /// Character saves: domain=Character, characterAttribute=Dexterity (tests a character's resilience).
    /// 
    /// Serializable for use on Skill ScriptableObjects.
    /// </summary>
    [System.Serializable]
    public struct SaveSpec
    {
        [Tooltip("Does this save test the vehicle or a character?")]
        public CheckDomain domain;
        
        [Tooltip("Vehicle attribute to save against (when domain = Vehicle)")]
        public VehicleCheckAttribute vehicleAttribute;
        
        [Tooltip("Character attribute to save against (when domain = Character)")]
        public CharacterAttribute characterAttribute;
        
        [Tooltip("Component that must be targeted for this save (None = any character can save)")]
        public ComponentType? requiredComponentType;
        
        /// <summary>Display-friendly name for logs and tooltips.</summary>
        public readonly string DisplayName => domain == CheckDomain.Character
            ? characterAttribute.ToString()
            : vehicleAttribute.ToString();
        
        public readonly bool IsCharacterSave => domain == CheckDomain.Character;
        public readonly bool IsVehicleSave => domain == CheckDomain.Vehicle;
        public readonly bool RequiresComponent => requiredComponentType.HasValue;
        
        // ==================== FACTORIES ====================
        
        public static SaveSpec ForVehicle(VehicleCheckAttribute attribute)
        {
            return new SaveSpec
            {
                domain = CheckDomain.Vehicle,
                vehicleAttribute = attribute,
                requiredComponentType = null
            };
        }
        
        public static SaveSpec ForCharacter(
            CharacterAttribute attribute,
            ComponentType? requiredComponent = null)
        {
            return new SaveSpec
            {
                domain = CheckDomain.Character,
                characterAttribute = attribute,
                requiredComponentType = requiredComponent
            };
        }
        
        public static SaveSpec None => ForVehicle(default);
    }
}