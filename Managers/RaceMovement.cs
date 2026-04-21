using Assets.Scripts.Entities.Vehicles;
using Assets.Scripts.Stages;
using Assets.Scripts.Stages.Lanes;
using Assets.Scripts.Conditions;
using UnityEngine;

namespace Assets.Scripts.Managers
{
    /// <summary>
    /// Race-traversal operations: moving vehicles along the track, handling stage
    /// transitions, and querying movement eligibility. 
    /// </summary>
    public static class RaceMovement
    {
        // ==================== MOVEMENT ====================

        /// <summary>Movement is paid for at turn start, and executed during turn or at turn end.</summary>
        public static bool ExecuteMovement(Vehicle vehicle)
        {
            if (vehicle == null) return false;

            if (vehicle.HasMovedThisTurn)
            {
                Debug.LogWarning($"[RaceMovement] {vehicle.vehicleName} has already moved this turn");
                return false;
            }

            if (!vehicle.CanMove())
            {
                string reason = vehicle.GetCannotMoveReason();
                TurnEventBus.Emit(new MovementBlockedEvent(vehicle, reason));
                vehicle.MarkMoved();
                return false;
            }

            var drive = vehicle.Drive;
            int distance = drive != null ? drive.GetCurrentSpeed() : 0;

            vehicle.MarkMoved();

            if (distance > 0)
            {
                int oldProgress = RacePositionTracker.GetProgress(vehicle);
                SetProgress(vehicle, oldProgress + distance);
                TurnEventBus.Emit(new MovementExecutedEvent(vehicle, distance, drive.GetCurrentSpeed(), oldProgress, RacePositionTracker.GetProgress(vehicle)));
                vehicle.NotifyStatusEffectTrigger(RemovalTrigger.OnMovement);
            }

            return true;
        }

        /// <summary>Sets a vehicle's progress to the given value and immediately resolves any stage transitions.</summary>
        public static void SetProgress(Vehicle vehicle, int progress)
        {
            if (vehicle == null) return;
            RacePositionTracker.SetProgress(vehicle, Mathf.Max(0, progress));
            TryHandleStageTransitions(vehicle);
        }

        /// <summary>
        /// Checks whether the vehicle has overshot its current stage boundary and transitions it forward.
        /// Loops to handle chained transitions when excess progress carries through multiple stages.
        /// Called after movement and after turn-end as a safety net for non-movement progress changes.
        /// </summary>
        private static void TryHandleStageTransitions(Vehicle vehicle)
        {
            if (vehicle == null || RacePositionTracker.GetStage(vehicle) == null) return;

            Stage currentStage;
            while ((currentStage = RacePositionTracker.GetStage(vehicle)) != null &&
                   RacePositionTracker.GetProgress(vehicle) >= currentStage.length)
            {
                StageLane currentLane = RacePositionTracker.GetLane(vehicle);
                Stage nextStage = null;

                TrackDefinition track = TrackDefinition.Active;
                if (track != null)
                {
                    nextStage = track.GetNextStage(currentLane);
                }
                else
                {
                    Debug.LogWarning("[RaceMovement] No active TrackDefinition — stage transition cannot proceed.");
                }

                if (nextStage != null)
                    MoveToStage(vehicle, nextStage);
                else
                {
                    if (TrackDefinition.IsFinish(currentStage))
                        TurnEventBus.Emit(new FinishLineCrossedEvent(vehicle, currentStage));
                    break;
                }
            }
        }

        private static void MoveToStage(Vehicle vehicle, Stage stage)
        {
            if (vehicle == null || stage == null) return;

            Stage previousStage = RacePositionTracker.GetStage(vehicle);
            StageLane currentLane = RacePositionTracker.GetLane(vehicle);
            TrackDefinition track = TrackDefinition.Active;
            StageLane targetLane = track != null ? track.GetTargetLane(currentLane) : null;

            if (previousStage != null)
                previousStage.TriggerLeave(vehicle);

            int carry = RacePositionTracker.GetProgress(vehicle) - (previousStage != null ? previousStage.length : 0);
            RacePositionTracker.SetProgress(vehicle, carry);
            RacePositionTracker.SetStage(vehicle, stage);

            stage.TriggerEnter(vehicle, targetLane);

            TurnEventBus.Emit(new StageEnteredEvent(vehicle, stage, previousStage, RacePositionTracker.GetProgress(vehicle)));
        }
    }
}
