using UnityEngine;
using SerializeReferenceEditor;
using Assets.Scripts.Characters;
using Assets.Scripts.Combat.Rolls.Advantage;
using Assets.Scripts.Entities.Vehicle;

namespace Assets.Scripts.Combat.Rolls.RollSpecs.SpecTypes
{
    /// <summary>
    /// What a skill check tests — either a vehicle attribute or a character skill.
    /// </summary>
    [System.Serializable]
    [SRName("Skill Check")]
    public class SkillCheckSpec : IRollSpec
    {
        [Header("Difficulty")]
        [Tooltip("Difficulty class to beat.")]
        public int dc = 15;

        [Header("Advantage / Disadvantage")]
        [Tooltip("Advantage or disadvantage granted by this spec. Normal = no grant.")]
        public RollMode grantedMode;

        [Header("Check")]
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
        public string DisplayName => domain == CheckDomain.Character
            ? characterSkill.ToString()
            : vehicleAttribute.ToString();

        public bool IsCharacterCheck => domain == CheckDomain.Character;
        public bool IsVehicleCheck => domain == CheckDomain.Vehicle;
        public bool RequiresComponent => requiresComponent;
        
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
    }
}
