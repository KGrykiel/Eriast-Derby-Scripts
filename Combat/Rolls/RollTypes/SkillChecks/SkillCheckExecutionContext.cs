using Assets.Scripts.Combat.Rolls.RollSpecs.SpecTypes;
using UnityEngine;

namespace Assets.Scripts.Combat.Rolls.RollTypes.SkillChecks
{
    /// <summary>
    /// Runtime execution context for skill checks.
    /// Bundles all arguments needed by SkillCheckPerformer.Execute().
    /// Not serialized — constructed fresh at call time.
    /// </summary>
    public class SkillCheckExecutionContext
    {
        /// <summary>Vehicle making the check. Required.</summary>
        public Vehicle Vehicle;

        /// <summary>Check specification — what skill/attribute to test.</summary>
        public SkillCheckSpec Spec;

        /// <summary>What triggered this check (Skill, EventCard, LaneTurnEffect, etc.). Used for logging.</summary>
        public Object CausalSource;

        /// <summary>Specific character initiating the check (for skills). Null for event cards/lane effects (routes to best modifier).</summary>
        public Character InitiatingCharacter;
    }
}
