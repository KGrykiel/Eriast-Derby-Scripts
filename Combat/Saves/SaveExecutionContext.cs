using Assets.Scripts.Combat.RollSpecs;
using UnityEngine;

namespace Assets.Scripts.Combat.Saves
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

        /// <summary>Difficulty class to beat.</summary>
        public int DC;

        /// <summary>What triggered this save (Skill, EventCard, StatusEffect, etc.). Used for logging.</summary>
        public Object CausalSource;

        /// <summary>Specific component being targeted (for routing). Null for vehicle-wide saves.</summary>
        public VehicleComponent TargetComponent;

        /// <summary>Entity that triggered the save (e.g., attacking weapon). Used for logging. Null if environmental.</summary>
        public Entity AttackerEntity;
    }
}
