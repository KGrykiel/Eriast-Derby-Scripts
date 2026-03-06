using System;
using SerializeReferenceEditor;
using UnityEngine;

namespace Assets.Scripts.Combat.RollSpecs
{
    /// <summary>
    /// Attack roll specification — designer-configured fields for attack rolls.
    /// Part of IRollSpec hierarchy, serialized in RollNode.
    /// </summary>
    [Serializable]
    [SRName("Attack")]
    public class AttackSpec : IRollSpec
    {
        [Range(0, 10)]
        [Tooltip("Penalty applied to the fallback roll when missing a component and attacking chassis instead.")]
        public int componentTargetingPenalty = 2;
    }
}
