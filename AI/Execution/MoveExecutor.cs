using Assets.Scripts.Entities.Vehicles;

namespace Assets.Scripts.AI.Execution
{
    /// <summary>
    /// End-of-seat-turn movement for driver seats. Phase 1 simplification: movement
    /// always fires at the end of a vehicle's turn, regardless of tactical context.
    ///
    /// Does not implement <see cref="IExecutor"/> because movement is not a scored
    /// <see cref="AIAction"/> — it is an unconditional end-of-turn step. More
    /// sophisticated driver movement timing is deferred (see AI-new.md, Phase 4).
    /// </summary>
    public class MoveExecutor
    {
        public void Execute(Vehicle vehicle, TurnService turnService)
        {
            if (vehicle == null || turnService == null) return;
            turnService.ExecuteMovement(vehicle);
        }
    }
}
