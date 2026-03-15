using Assets.Scripts.Combat.RollSpecs;
using UnityEngine;

namespace Assets.Scripts.Combat.OpposedChecks
{
    /// <summary>
    /// Runtime execution context for opposed checks.
    /// Bundles all arguments needed by OpposedCheckPerformer.Execute().
    /// Not serialized — constructed fresh at call time.
    /// </summary>
    public class OpposedCheckExecutionContext
    {
        /// <summary>Vehicle initiating the contest. Required.</summary>
        public Vehicle AttackerVehicle;

        /// <summary>Vehicle resisting the contest. Required.</summary>
        public Vehicle DefenderVehicle;

        /// <summary>Opposed check specification — what each side rolls.</summary>
        public OpposedCheckRollSpec Spec;

        /// <summary>What triggered this check (Skill, EventCard, etc.). Used for logging.</summary>
        public Object CausalSource;

        /// <summary>Specific character initiating the check (for skills). Null for event cards/lane effects.</summary>
        public Character AttackerCharacter;
    }
}
