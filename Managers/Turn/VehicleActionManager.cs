using System;
using System.Collections;
using Assets.Scripts.Entities.Vehicles;
using Assets.Scripts.Entities.Vehicles.VehicleComponents;
using Assets.Scripts.Managers.Turn.TurnPhases;
using Assets.Scripts.Skills;
using Assets.Scripts.Visualisation;
using UnityEngine;

namespace Assets.Scripts.Managers.Turn
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

        public bool IsPaused { get; set; }

        public VehicleActionManager(MonoBehaviour coroutineRunner)
        {
            this.coroutineRunner = coroutineRunner;
        }

        /// <summary>
        /// Executes an auto-movement action: respects pause, executes movement, then waits one action delay before calling back.
        /// </summary>
        public void ExecuteMovementAction(Vehicle vehicle, Action movement, Action onComplete)
        {
            coroutineRunner.StartCoroutine(MovementActionRoutine(vehicle, movement, onComplete));
        }

        private IEnumerator MovementActionRoutine(Vehicle vehicle, Action movement, Action onComplete)
        {
            yield return new WaitUntil(() => !IsPaused);

            VehicleVisual visual = vehicle.GetComponent<VehicleVisual>();
            if (visual != null)
                visual.ShowActionLabel("Moving");

            yield return new WaitForSeconds(actionDelay);
            yield return new WaitUntil(() => !IsPaused);

            if (visual != null)
                visual.HideActionLabel();

            movement();

            yield return new WaitForSeconds(actionDelay);
            yield return new WaitUntil(() => !IsPaused);

            onComplete();
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
            yield return new WaitUntil(() => !IsPaused);

            ShowActionVisuals(action);

            yield return new WaitForSeconds(actionDelay);
            yield return new WaitUntil(() => !IsPaused);

            HideActionVisuals(action);
            SkillPipeline.Execute(action);

            yield return new WaitForSeconds(actionDelay);
            yield return new WaitUntil(() => !IsPaused);

            onComplete();
        }

        private static void ShowActionVisuals(SkillAction action)
        {
            VehicleVisual sourceVisual = GetSourceVisual(action);
            if (sourceVisual == null)
                return;

            VehicleVisual targetVisual = GetTargetVisual(action);
            if (targetVisual != null)
                sourceVisual.ShowActionLine(targetVisual);

            sourceVisual.ShowActionLabel(action.skill.name);
        }

        private static void HideActionVisuals(SkillAction action)
        {
            VehicleVisual sourceVisual = GetSourceVisual(action);
            if (sourceVisual == null)
                return;

            sourceVisual.HideActionLine();
            sourceVisual.HideActionLabel();
        }

        private static VehicleVisual GetSourceVisual(SkillAction action)
        {
            Vehicle source = action.sourceActor.GetVehicle();
            if (source == null)
                return null;

            return source.GetComponent<VehicleVisual>();
        }

        private static VehicleVisual GetTargetVisual(SkillAction action)
        {
            if (action.target is Vehicle targetVehicle)
                return targetVehicle.GetComponent<VehicleVisual>();

            if (action.target is VehicleComponent targetComponent)
                return targetComponent.ParentVehicle != null
                    ? targetComponent.ParentVehicle.GetComponent<VehicleVisual>()
                    : null;

            return null;
        }
    }
}
