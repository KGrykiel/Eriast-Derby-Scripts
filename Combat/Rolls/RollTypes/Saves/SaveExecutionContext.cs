using Assets.Scripts.Combat;
using Assets.Scripts.Combat.Rolls.RollSpecs.SpecTypes;

namespace Assets.Scripts.Combat.Rolls.RollTypes.Saves
{
    /// <summary>
    /// Runtime execution context for saving throws.
    /// Bundles all arguments needed by SavePerformer.Execute().
    /// Not serialized — constructed fresh at call time.
    /// </summary>
    public class SaveExecutionContext
    {
        /// <summary>Vehicle making the save. Required.</summary>
        public Vehicle Vehicle;

        /// <summary>Save specification — what attribute/skill to test.</summary>
        public SaveSpec Spec;

        /// <summary>What triggered this save (Skill, EventCard, StatusEffect, etc.). Used for logging.</summary>
        public string CausalSource;

        /// <summary>Pre-resolved routing result. Must not be null.</summary>
        public CheckRouter.RoutingResult Routing;

        /// <summary>Entity that triggered the save (e.g., attacking weapon). Used for logging. Null if environmental.</summary>
        public Entity AttackerEntity;
    }
}
