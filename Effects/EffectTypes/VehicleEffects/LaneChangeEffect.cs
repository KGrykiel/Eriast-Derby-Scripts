using System;
using Assets.Scripts.Combat;
using Assets.Scripts.Entities.Vehicles;
using Assets.Scripts.Managers.Race;
using Assets.Scripts.Stages.Lanes;
using SerializeReferenceEditor;
using UnityEngine;

namespace Assets.Scripts.Effects.EffectTypes.VehicleEffects
{
    /// <summary>Moves a vehicle a number of lanes relative to its current position.</summary>
    [Serializable]
    [SRName("Lane Change/Relative")]
    public class RelativeLaneChangeEffect : IVehicleEffect
    {
        [Tooltip("Lane offset to apply (-1 = left, +1 = right).")]
        [Range(-2, 2)]
        public int relativeOffset = 1;

        void IVehicleEffect.Apply(Vehicle target, EffectContext context)
        {
            var stage = RacePositionTracker.GetStage(target);
            StageLane fromLane = RacePositionTracker.GetLane(target);
            int currentIndex = stage.GetLaneIndex(fromLane);
            if (currentIndex < 0)
            {
                Debug.LogWarning($"[RelativeLaneChangeEffect] Current lane of '{target.name}' was not found in stage '{stage.name}'.");
                return;
            }

            var targetLane = stage.GetLaneByIndex(currentIndex + relativeOffset);
            if (targetLane == null)
            {
                Debug.LogWarning($"[RelativeLaneChangeEffect] Offset {relativeOffset} from lane {currentIndex} is out of bounds — effect had no impact.");
                return;
            }

            stage.AssignVehicleToLane(target, targetLane);
            CombatEventBus.Emit(new LaneChangeEvent(target, fromLane, targetLane, context.CausalSource));
        }
    }

    /// <summary>Moves a vehicle to a specific lane index regardless of current position.</summary>
    [Serializable]
    [SRName("Lane Change/Absolute")]
    public class AbsoluteLaneChangeEffect : IVehicleEffect
    {
        [Tooltip("Target lane index to move to.")]
        public int targetLaneIndex = 0;

        void IVehicleEffect.Apply(Vehicle target, EffectContext context)
        {
            var stage = RacePositionTracker.GetStage(target);
            StageLane fromLane = RacePositionTracker.GetLane(target);
            var toLane = stage.GetLaneByIndex(targetLaneIndex);
            if (toLane == null)
            {
                Debug.LogWarning($"[AbsoluteLaneChangeEffect] Lane index {targetLaneIndex} does not exist in stage '{stage.name}' — effect had no impact.");
                return;
            }

            stage.AssignVehicleToLane(target, toLane);
            CombatEventBus.Emit(new LaneChangeEvent(target, fromLane, toLane, context.CausalSource));
        }
    }
}