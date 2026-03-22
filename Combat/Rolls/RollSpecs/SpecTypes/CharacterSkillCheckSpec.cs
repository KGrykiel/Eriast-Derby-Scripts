using UnityEngine;
using SerializeReferenceEditor;
using Assets.Scripts.Characters;

namespace Assets.Scripts.Combat.Rolls.RollSpecs.SpecTypes
{
    [System.Serializable]
    [SRName("Character Check")]
    public sealed class CharacterSkillCheckSpec : SkillCheckSpec
    {
        [Tooltip("Character skill to check.")]
        public CharacterSkill characterSkill;

        [Tooltip("Does this check require a specific component type?")]
        public bool requiresComponent;

        [Tooltip("Component type required (only used if requiresComponent is true).")]
        public ComponentType requiredComponentType;

        public bool RequiresComponent => requiresComponent;

        public override string DisplayName => characterSkill.ToString();
    }
}
