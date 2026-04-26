using System;
using System.Collections.Generic;
using Assets.Scripts.Combat.Rolls.Targeting;
using Assets.Scripts.Effects.Invocations;
using SerializeReferenceEditor;
using UnityEngine;

namespace Assets.Scripts.Combat.Rolls.RollSpecs
{
    /// <summary>
    /// A self-contained resolution step: optionally make a roll, then apply success or failure effects.
    /// Nodes can chain — onSuccessChains/onFailureChains execute depth-first after this node resolves.
    /// Each list entry runs fully (including its own chains) before the next entry starts.
    /// Used by Skill, CardChoice, and LaneTurnEffect as a unified resolution unit.
    /// </summary>
    [Serializable]
    [SRName("Roll Node")]
    public class RollNode
    {
        [SerializeReference, SR]
        [Tooltip("What kind of roll this node requires. Leave null for an unconditional effect (always succeeds).")]
        public IRollSpec rollSpec;

        [SerializeReference, SR]
        [Tooltip("Required: resolves which targets this node fans out to. Use CurrentTargetResolver for single-target execution. Null will log a warning and skip the node.")]
        public IRollTargetResolver targetResolver;

        [Header("Effects")]
        [SerializeReference, SR]
        [Tooltip("Effects applied on a successful roll, or always if rollSpec is null.")]
        public List<IEffectInvocation> successEffects = new();

        [SerializeReference, SR]
        [Tooltip("Effects applied on a failed roll. Ignored if rollSpec is null.")]
        public List<IEffectInvocation> failureEffects = new();

        [Header("Chaining")]
        [SerializeReference, SR]
        [Tooltip("Nodes evaluated in order after this one succeeds. Each resolves fully before the next begins.")]
        public List<RollNode> onSuccessChains = new();

        [SerializeReference, SR]
        [Tooltip("Nodes evaluated in order after this one fails. Each resolves fully before the next begins.")]
        public List<RollNode> onFailureChains = new();
    }
}
