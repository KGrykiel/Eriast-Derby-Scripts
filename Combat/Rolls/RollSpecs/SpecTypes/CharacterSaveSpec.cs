using UnityEngine;
using SerializeReferenceEditor;
using Assets.Scripts.Characters;

namespace Assets.Scripts.Combat.Rolls.RollSpecs.SpecTypes
{
    [System.Serializable]
    [SRName("Character Save")]
    public sealed class CharacterSaveSpec : SaveSpec
    {
        [Tooltip("Character attribute to save against.")]
        public CharacterAttribute characterAttribute;

        [Tooltip("Does this save require a specific component?")]
        public bool requiresComponent;

        [Tooltip("Component type required (only used if requiresComponent is true).")]
        public ComponentType requiredComponentType;

        public bool RequiresComponent => requiresComponent;

        public override string DisplayName => characterAttribute.ToString();
    }
}
