using UnityEngine;
using Assets.Scripts.Characters;
using Assets.Scripts.Combat.Rolls.Advantage;
using Assets.Scripts.Entities.Vehicles;
using Assets.Scripts.Entities.Vehicles.VehicleComponents;

namespace Assets.Scripts.Combat.Rolls.RollSpecs.SpecTypes
{
    /// <summary>
    /// What a skill check tests — either a vehicle attribute or a character skill.
    /// </summary>
    [System.Serializable]
    public abstract class SkillCheckSpec : IRollSpec
    {
        [Header("Difficulty")]
        [Tooltip("Difficulty class to beat.")]
        public int dc = 15;

        [Header("Advantage / Disadvantage")]
        [Tooltip("Advantage or disadvantage granted by this spec. Normal = no grant.")]
        public RollMode grantedMode;

        [Header("Roller")]
        [Tooltip("Who makes this check — the source of the action or its target. Default is Source (source acts on the world).")]
        public RollerSource roller = RollerSource.Source;

        /// <summary>Display-friendly name for logs and tooltips.</summary>
        public abstract string DisplayName { get; }

        // ==================== FACTORIES ====================

        public static VehicleSkillCheckSpec ForVehicle(VehicleCheckAttribute attribute)
        {
            return new VehicleSkillCheckSpec
            {
                vehicleAttribute = attribute
            };
        }

        public static CharacterSkillCheckSpec ForCharacter(CharacterSkill skill, ComponentType? requiredComponent = null)
        {
            return new CharacterSkillCheckSpec
            {
                characterSkill = skill,
                requiresComponent = requiredComponent.HasValue,
                requiredComponentType = requiredComponent ?? default
            };
        }
    }
}
