using Assets.Scripts.Entities.Vehicles;
using Assets.Scripts.Stages.Lanes;
using SerializeReferenceEditor;
using UnityEngine;

namespace Assets.Scripts.Effects.EffectTypes
{
    /// <summary>Effect for vehicle changing lanes. Accepts Vehicle directly or Entity (resolves to parent vehicle).</summary>
    [System.Serializable]
    [SRName("Lane Change")]
    public class LaneChangeEffect : EffectBase
    {
        [Header("Target Lane")]
        [Tooltip("Absolute lane index to move to (ignored if using relative offset)")]
        public int targetLaneIndex = 0;

        [Tooltip("Use relative offset instead of absolute index? (e.g., +1 = move one lane right)")]
        public bool useRelativeOffset = false;

        [Tooltip("Relative lane offset (-1 = left, +1 = right). Only used if useRelativeOffset is true")]
        [Range(-2, 2)]
        public int relativeOffset = 1;

        public override void Apply(IEffectTarget target, EffectContext context)
        {
            Vehicle vehicle = ResolveVehicle(target);
            if (vehicle == null || vehicle.currentStage == null) return;

            StageLane targetLane = DetermineTargetLane(vehicle);
            if (targetLane == null) return;

            vehicle.currentStage.AssignVehicleToLane(vehicle, targetLane);
        }

        private StageLane DetermineTargetLane(Vehicle vehicle)
        {
            var stage = vehicle.currentStage;
            int targetIndex;

            if (useRelativeOffset)
            {
                // Relative: Add offset to current lane index
                if (vehicle.currentLane == null)
                    return null;

                int currentIndex = stage.GetLaneIndex(vehicle.currentLane);
                if (currentIndex < 0)
                    return null;

                targetIndex = currentIndex + relativeOffset;
            }
            else
            {
                // Absolute: Use specified index
                targetIndex = targetLaneIndex;
            }

            // GetLaneByIndex handles bounds checking
            return stage.GetLaneByIndex(targetIndex);
        }
    }
}