using System.Collections.Generic;
using Assets.Scripts.Conditions;
using SerializeReferenceEditor;
using UnityEngine;

namespace Assets.Scripts.Conditions.CharacterConditions
{
    /// <summary>
    /// Template asset for character-domain conditions (e.g., Stunned, Inspired, Blinded).
    /// Characters have no HP/Energy resources, so no periodic effects or EntityFeature requirements.
    /// </summary>
    [CreateAssetMenu(menuName = "Racing/Character Condition", fileName = "New Character Condition")]
    public class CharacterCondition : ConditionBase
    {
        [Header("Character Modifiers")]
        [Tooltip("Skill/attribute modifiers applied while this condition is active")]
        [SerializeReference, SR]
        public List<CharacterModifierData> modifiers = new();
    }
}
