using System;
using UnityEngine;
using Assets.Scripts.Characters;
using SerializeReferenceEditor;

namespace Assets.Scripts.Conditions.CharacterConditions
{
    [Serializable]
    public abstract class CharacterModifierData
    {
        [Tooltip("Flat adds a fixed amount; Multiplier scales the value")]
        public ModifierType type;
        [Tooltip("Amount to modify by (negative for penalties)")]
        public float value;
    }

    [Serializable]
    [SRName("Character Skill Modifier")]
    public class CharacterSkillModifierData : CharacterModifierData
    {
        [Tooltip("Character skill to modify (e.g., Piloting, Engineering)")]
        public CharacterSkill skill;
    }

    [Serializable]
    [SRName("Character Attribute Modifier")]
    public class CharacterAttributeModifierData : CharacterModifierData
    {
        [Tooltip("Character attribute to modify (e.g., STR, DEX saves)")]
        public CharacterAttribute attribute;
    }
}

