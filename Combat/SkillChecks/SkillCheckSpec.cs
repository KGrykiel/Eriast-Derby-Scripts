using UnityEngine;
using Assets.Scripts.Characters;
using Assets.Scripts.Entities.Vehicle;

namespace Assets.Scripts.Combat.SkillChecks
{
    /// <summary>
    /// Describes what a skill check tests — either a vehicle attribute or a character skill.
    /// Replaces the flat SkillCheckType enum, eliminating redundant CharacterSkill mirrors.
    /// 
    /// Vehicle checks: domain=Vehicle, vehicleAttribute=Mobility (tests the vehicle's own stat).
    /// Character checks: domain=Character, characterSkill=Piloting (tests a character's training).
    /// 
    /// Serializable for use on Skill ScriptableObjects.
    /// </summary>
    [System.Serializable]
    public struct SkillCheckSpec
    {
        [Tooltip("Does this check test the vehicle or a character?")]
        public CheckDomain domain;
        
        [Tooltip("Vehicle attribute to check (when domain = Vehicle)")]
        public VehicleCheckAttribute vehicleAttribute;
        
        [Tooltip("Character skill to check (when domain = Character)")]
        public CharacterSkill characterSkill;
        
        [Tooltip("Does this check require a specific component type?")]
        public bool requiresComponent;

        [Tooltip("Component type required (only used if requiresComponent is true)")]
        public ComponentType requiredComponentType;

        /// <summary>Display-friendly name for logs and tooltips.</summary>
        public readonly string DisplayName => domain == CheckDomain.Character
            ? characterSkill.ToString()
            : vehicleAttribute.ToString();

        public readonly bool IsCharacterCheck => domain == CheckDomain.Character;
        public readonly bool IsVehicleCheck => domain == CheckDomain.Vehicle;
        public readonly bool RequiresComponent => requiresComponent;
        
        // ==================== FACTORIES ====================
        
        public static SkillCheckSpec ForVehicle(VehicleCheckAttribute attribute)
        {
            return new SkillCheckSpec
            {
                domain = CheckDomain.Vehicle,
                vehicleAttribute = attribute,
                requiresComponent = false
            };
        }
        
        public static SkillCheckSpec ForCharacter(CharacterSkill skill, ComponentType? requiredComponent = null)
        {
            return new SkillCheckSpec
            {
                domain = CheckDomain.Character,
                characterSkill = skill,
                requiresComponent = requiredComponent.HasValue,
                requiredComponentType = requiredComponent ?? default
            };
        }
        
        public static SkillCheckSpec None => ForVehicle(default);
    }
}
