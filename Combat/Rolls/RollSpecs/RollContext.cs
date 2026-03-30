using Assets.Scripts.Entities;

namespace Assets.Scripts.Combat.Rolls.RollSpecs
{
    /// <summary>
    /// Context struct to hold any relevant information about the action being executed.
    /// Used by skills, event cards, lane effects, and any future action sources.
    /// SourceActor is set for player skills; null for event cards and lane effects.
    /// Source vehicle is derived: ctx.SourceActor?.GetVehicle() ?? ctx.Target as Vehicle.
    /// </summary>
    public struct RollContext
    {
        public IRollTarget Target;
        public RollActor SourceActor;
        /// <summary>What triggered this roll, used for logging. Set by the caller before passing to RollNodeExecutor.</summary>
        public string CausalSource;
    }
}
