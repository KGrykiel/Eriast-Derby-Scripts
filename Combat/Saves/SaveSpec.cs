using UnityEngine;
using Assets.Scripts.Characters;
using Assets.Scripts.Entities.Vehicle;

namespace Assets.Scripts.Combat.Saves
{
    /// <summary>
    /// What a saving throw tests — either a vehicle attribute or a character attribute.
    /// Right now it handles both with the other being null, but might think of a better system for this if it gets more complicated.
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
        
        [Tooltip("Does this save require a specific component?")]
        public bool requiresComponent;

        [Tooltip("Component type required (only used if requiresComponent is true)")]
        public ComponentType requiredComponentType;

        /// <summary>Display-friendly name for logs and tooltips.</summary>
        public readonly string DisplayName => domain == CheckDomain.Character
            ? characterAttribute.ToString()
            : vehicleAttribute.ToString();

        public readonly bool IsCharacterSave => domain == CheckDomain.Character;
        public readonly bool IsVehicleSave => domain == CheckDomain.Vehicle;
        public readonly bool RequiresComponent => requiresComponent;
        
        // ==================== FACTORIES ====================
        
        public static SaveSpec ForVehicle(VehicleCheckAttribute attribute)
        {
            return new SaveSpec
            {
                domain = CheckDomain.Vehicle,
                vehicleAttribute = attribute,
                requiresComponent = false
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
                requiresComponent = requiredComponent.HasValue,
                requiredComponentType = requiredComponent ?? default
            };
        }
        
        public static SaveSpec None => ForVehicle(default);
    }
}