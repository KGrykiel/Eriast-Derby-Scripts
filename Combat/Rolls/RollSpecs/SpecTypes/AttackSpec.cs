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
    }
}
