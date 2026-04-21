using System;
using System.Collections;
using Assets.Scripts.Entities.Vehicles;
using Assets.Scripts.Managers.TurnPhases;
using Assets.Scripts.Skills;
using UnityEngine;

namespace Assets.Scripts.Managers
{
    /// <summary>
    /// Central action loop. Asks a controller for one action at a time, plays the
    /// pre-execution delay, executes it, then asks again until the controller is done.
    /// This is the home of stepped playback, inter-action delays, and visual hooks.
    /// </summary>
    public class VehicleActionManager
    {
        private readonly MonoBehaviour coroutineRunner;

        [Tooltip("Seconds to wait before executing each action.")]
        public float actionDelay = 0.6f;

        public VehicleActionManager(MonoBehaviour coroutineRunner)
        {
            this.coroutineRunner = coroutineRunner;
        }

        public void ExecuteTurn(Vehicle vehicle, IVehicleTurnController controller, TurnPhaseContext context, Action onTurnDone)
        {
            RequestNext(vehicle, controller, context, onTurnDone);
        }

        private void RequestNext(Vehicle vehicle, IVehicleTurnController controller, TurnPhaseContext context, Action onTurnDone)
        {
            controller.RequestNextAction(vehicle, context,
                onAction: action =>
                {
                    coroutineRunner.StartCoroutine(ExecuteAfterDelay(action, () =>
                        RequestNext(vehicle, controller, context, onTurnDone)));
                },
                onDone: onTurnDone);
        }

        private IEnumerator ExecuteAfterDelay(SkillAction action, Action onComplete)
        {
            yield return new WaitForSeconds(actionDelay);
            // Future: visual effects, screen shake, attack line rendering here.
            SkillPipeline.Execute(action);
            onComplete();
        }
    }
}
