using UnityEngine;
using SerializeReferenceEditor;
using Assets.Scripts.Characters;
using Assets.Scripts.Entities.Vehicles.VehicleComponents;

namespace Assets.Scripts.Combat.Rolls.RollSpecs.SpecTypes
{
    [System.Serializable]
    [SRName("Character Check")]
    public sealed class CharacterSkillCheckSpec : SkillCheckSpec
    {
        [Tooltip("Character skill to check.")]
        public CharacterSkill characterSkill;

        [Tooltip("Required crew role for this check (None = no component required).")]
        public RoleType requiredRole = RoleType.None;

        public bool RequiresComponent => requiredRole != RoleType.None;

        public override string DisplayName => characterSkill.ToString();
    }
}
