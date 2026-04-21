using Assets.Scripts.Combat.Rolls.RollSpecs;
using Assets.Scripts.Entities.Vehicles;

namespace Assets.Scripts.Skills
{
    /// <summary>
    /// Shared execution pipeline for all skill actions, regardless of source (player or AI).
    /// Builds the <see cref="RollContext"/> and delegates to the vehicle's skill execution.
    /// </summary>
    public static class SkillPipeline
    {
        public static void Execute(SkillAction action)
        {
            if (action == null || action.skill == null || action.sourceActor == null) return;

            Vehicle vehicle = action.sourceActor.GetVehicle();
            if (vehicle == null) return;

            var ctx = new RollContext
            {
                SourceActor = action.sourceActor,
                Target = action.target,
                CausalSource = action.skill.name
            };

            vehicle.ExecuteSkill(ctx, action.skill);
        }
    }
}
