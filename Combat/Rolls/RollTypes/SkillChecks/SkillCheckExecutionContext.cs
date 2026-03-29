using Assets.Scripts.Combat.Rolls.RollSpecs.SpecTypes;
using Assets.Scripts.Entities.Vehicles;

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
        public string CausalSource;

        /// <summary>Pre-resolved routing result. Must not be null.</summary>
        public CheckRouter.RoutingResult Routing;
    }
}
