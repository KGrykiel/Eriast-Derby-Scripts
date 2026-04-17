using Assets.Scripts.Combat.Rolls.RollSpecs;
using Assets.Scripts.Entities.Vehicles;
using UnityEngine;

namespace Assets.Scripts.AI.Execution
{
    /// <summary>
    /// Fires the selected skill through the same pipeline the player uses.
    /// Mirrors <c>PlayerController.ExecuteSkill()</c>: builds a <see cref="RollContext"/>
    /// from the pre-built actor and target on the action, then delegates to
    /// <c>Vehicle.ExecuteSkill</c>. All validation (costs, action economy,
    /// targeting) is re-checked there — the executor itself adds nothing.
    /// </summary>
    public class SkillExecutor : IExecutor
    {
        public void Execute(AIAction action, TurnService turnService)
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
