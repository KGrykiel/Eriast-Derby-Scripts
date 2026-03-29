using System;
using Assets.Scripts.Entities.Vehicles;
using SerializeReferenceEditor;
using UnityEngine;

namespace Assets.Scripts.Combat.Rolls.RollSpecs.SpecTypes
{
    /// <summary>
    /// Threshold check against a live vehicle state variable — passes if the current value meets or
    /// exceeds the minimum. Not a d20 roll; deterministic pass/fail with no modifier influence.
    /// </summary>
    [Serializable]
    [SRName("State Threshold")]
    public class StateThresholdSpec : IRollSpec
    {
        [Tooltip("Which live vehicle state to compare against the minimum.")]
        public Vehicle.RuntimeState state;

        [Tooltip("Minimum value required to pass. Fails if the current value is below this.")]
        public int minimumValue;
    }
}
