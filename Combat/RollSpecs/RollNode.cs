using System;
using System.Collections.Generic;
using SerializeReferenceEditor;
using UnityEngine;

namespace Assets.Scripts.Combat.RollSpecs
{
    /// <summary>
    /// A self-contained resolution step: optionally make a roll, then apply success or failure effects.
    /// Nodes can chain — onSuccessChain/onFailureChain execute after this node resolves.
    /// Used by Skill, CardChoice, and LaneTurnEffect as a unified resolution unit.
    /// </summary>
    [Serializable]
    [SRName("Roll Node")]
    public class RollNode
    {
        [SerializeReference, SR]
        [Tooltip("What kind of roll this node requires. Leave null for an unconditional effect (always succeeds).")]
        public IRollSpec rollSpec;

        [Tooltip("Difficulty class for skill checks and saves. Ignored by attacks (use target AC) and opposed checks (compare rolls).")] 
        public int dc = 15;

        [Header("Effects")]
        [Tooltip("Effects applied on a successful roll, or always if rollSpec is null.")]
        public List<EffectInvocation> successEffects = new();

        [Tooltip("Effects applied on a failed roll. Ignored if rollSpec is null.")]
        public List<EffectInvocation> failureEffects = new();

        [Header("Narrative")]
        [Tooltip("Text shown to the DM when this node succeeds.")]
        public string successNarrative = "";

        [Tooltip("Text shown to the DM when this node fails.")]
        public string failureNarrative = "";

        [Header("Chaining")]
        [SerializeReference, SR]
        [Tooltip("Node evaluated after this one succeeds. Null = stop here.")]
        public RollNode onSuccessChain;

        [SerializeReference, SR]
        [Tooltip("Node evaluated after this one fails. Null = stop here.")]
        public RollNode onFailureChain;
    }
}
