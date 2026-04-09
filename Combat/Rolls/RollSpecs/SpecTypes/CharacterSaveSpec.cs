using UnityEngine;
using SerializeReferenceEditor;
using Assets.Scripts.Characters;
using Assets.Scripts.Entities.Vehicles.VehicleComponents;

namespace Assets.Scripts.Combat.Rolls.RollSpecs.SpecTypes
{
    [System.Serializable]
    [SRName("Character Save")]
    public sealed class CharacterSaveSpec : SaveSpec
    {
        [Tooltip("Character attribute to save against.")]
        public CharacterAttribute characterAttribute;

        [Tooltip("Required crew role for this save (None = no component required).")]
        public RoleType requiredRole = RoleType.None;

        public bool RequiresComponent => requiredRole != RoleType.None;

        public override string DisplayName => characterAttribute.ToString();
    }
}
