using System;
using Assets.Scripts.Combat.Rolls.Advantage;
using SerializeReferenceEditor;
using UnityEngine;

namespace Assets.Scripts.Combat.Rolls.RollSpecs.SpecTypes
{
    /// <summary>
    /// Attack roll specification — designer-configured fields for attack rolls.
    /// Part of IRollSpec hierarchy, serialized in RollNode.
    /// </summary>
    [Serializable]
    [SRName("Attack")]
    public class AttackSpec : IRollSpec
    {
        [Header("Advantage / Disadvantage")]
        [Tooltip("Advantage or disadvantage granted by this spec. Normal = no grant.")]
        public RollMode grantedMode;

        [Header("Attack")]
        [Range(0, 10)]
        [Tooltip("Penalty applied to the fallback roll when missing a component and attacking chassis instead.")]
        public int componentTargetingPenalty = 2;
    }
}
