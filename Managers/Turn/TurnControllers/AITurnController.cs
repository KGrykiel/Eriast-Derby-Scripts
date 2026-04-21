using System;
using Assets.Scripts.AI;
using Assets.Scripts.Entities.Vehicles;
using Assets.Scripts.Managers.Turn.TurnPhases;
using Assets.Scripts.Skills;

namespace Assets.Scripts.Managers.Turn.TurnControllers
{
    /// <summary>
    /// Turn controller for AI-controlled vehicles. Drives VehicleAIComponent one
    /// step at a time, submitting each action to the VehicleActionManager until
    /// the vehicle has nothing left to do.
    /// </summary>
    public class AITurnController : IVehicleTurnController
    {
        public void RequestNextAction(Vehicle vehicle, TurnPhaseContext context, Action<SkillAction> onAction, Action onDone)
        {
            if (vehicle == null || !vehicle.IsOperational())
            {
                onDone();
                return;
            }

            var ai = vehicle.GetComponent<VehicleAIComponent>();
            if (ai == null)
            {
                onDone();
                return;
            }

            SkillAction action = ai.TakeOneStep(context.TurnController);
            if (action != null)
                onAction(action);
            else
                onDone();
        }
    }
}
