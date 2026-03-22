using Assets.Scripts.Combat;
using Assets.Scripts.Combat.Rolls.RollSpecs.SpecTypes;

namespace Assets.Scripts.Combat.Rolls.RollTypes.OpposedChecks
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
        public string CausalSource;

        /// <summary>Pre-resolved routing for the attacker. Must not be null.</summary>
        public CheckRouter.RoutingResult AttackerRouting;

        /// <summary>Pre-resolved routing for the defender. Must not be null.</summary>
        public CheckRouter.RoutingResult DefenderRouting;
    }
}
