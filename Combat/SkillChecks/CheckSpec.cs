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
    public struct CheckSpec
    {
        [Tooltip("Does this check test the vehicle or a character?")]
        public CheckDomain domain;
        
        [Tooltip("Vehicle attribute to check (when domain = Vehicle)")]
        public VehicleCheckAttribute vehicleAttribute;
        
        [Tooltip("Character skill to check (when domain = Character)")]
        public CharacterSkill characterSkill;
        
        /// <summary>Display-friendly name for logs and tooltips.</summary>
        public readonly string DisplayName => domain == CheckDomain.Character
            ? characterSkill.ToString()
            : vehicleAttribute.ToString();
        
        public readonly bool IsCharacterCheck => domain == CheckDomain.Character;
        public readonly bool IsVehicleCheck => domain == CheckDomain.Vehicle;
        
        // ==================== FACTORIES ====================
        
        public static CheckSpec ForVehicle(VehicleCheckAttribute attribute)
        {
            return new CheckSpec
            {
                domain = CheckDomain.Vehicle,
                vehicleAttribute = attribute
            };
        }
        
        public static CheckSpec ForCharacter(CharacterSkill skill)
        {
            return new CheckSpec
            {
                domain = CheckDomain.Character,
                characterSkill = skill
            };
        }
        
        public static CheckSpec None => ForVehicle(default);
    }
}
