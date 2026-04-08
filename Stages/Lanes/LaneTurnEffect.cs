using System;
using UnityEngine;
using SerializeReferenceEditor;
using Assets.Scripts.Combat.Rolls.RollSpecs;

namespace Assets.Scripts.Stages.Lanes
{
    /// <summary>
    /// Effect that triggers every turn for vehicles in a lane.
    /// </summary>
    [Serializable]
    public class LaneTurnEffect
    {
        [Header("Identity")]
        [Tooltip("Display name for this effect (e.g., 'Cliff Edge Hazard')")]
        public string effectName = "Lane Effect";

        [TextArea(2, 4)]
        [Tooltip("Narrative description shown when effect triggers")]
        public string description = "";

        [Tooltip("Text logged when the vehicle succeeds this effect.")]
        public string successNarrative = "";

        [Tooltip("Text logged when the vehicle fails this effect.")]
        public string failureNarrative = "";

        [SerializeReference, SR]
        [Tooltip("The full resolution of this effect: roll type, DC, success and failure effects, optional chain.")]
        public RollNode rollNode;
    }
}
