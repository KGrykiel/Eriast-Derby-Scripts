using System;
using Assets.Scripts.AI;
using Assets.Scripts.Entities.Vehicles;
using Assets.Scripts.Managers.TurnPhases;
using Assets.Scripts.Skills;

namespace Assets.Scripts.Managers
{
    /// <summary>
    /// Turn controller for AI-controlled vehicles. Drives VehicleAIComponent one
    /// step at a time, submitting each action to the VehicleActionManager until
    /// the vehicle has nothing left to do.
    /// </summary>
    public class AITurnController : IVehicleTurnController
    {
        public void BeginTurn(Vehicle vehicle, TurnPhaseContext context, Action onDone)
        {
            if (vehicle != null && vehicle.IsOperational())
            {
                var ai = vehicle.GetComponent<VehicleAIComponent>();
                if (ai != null)
                {
                    SkillAction action = ai.TakeOneStep(context.TurnController);
                    while (action != null)
                    {
                        context.ActionManager.Submit(action);
                        action = ai.TakeOneStep(context.TurnController);
                    }
                }
            }

            onDone();
        }
    }
}
