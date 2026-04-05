using Assets.Scripts.Entities.Vehicles;
using Assets.Scripts.Stages.Lanes;
using SerializeReferenceEditor;
using UnityEngine;

namespace Assets.Scripts.Effects.EffectTypes.VehicleEffects
{
    /// <summary>Effect for vehicle changing lanes. Accepts Vehicle directly or Entity (resolves to parent vehicle).</summary>
    [System.Serializable]
    [SRName("Lane Change")]
    public class LaneChangeEffect : IVehicleEffect
    {
        [Header("Target Lane")]
        [Tooltip("Absolute lane index to move to (ignored if using relative offset)")]
        public int targetLaneIndex = 0;

        [Tooltip("Use relative offset instead of absolute index? (e.g., +1 = move one lane right)")]
        public bool useRelativeOffset = false;

        [Tooltip("Relative lane offset (-1 = left, +1 = right). Only used if useRelativeOffset is true")]
        [Range(-2, 2)]
        public int relativeOffset = 1;

        void IVehicleEffect.Apply(Vehicle target, EffectContext context)
        {
            StageLane targetLane = DetermineTargetLane(target);
            if (targetLane == null)
            {
                Debug.LogWarning($"[LaneChangeEffect] Could not resolve a target lane for '{target.name}' — effect had no impact.");
                return;
            }

            target.CurrentStage.AssignVehicleToLane(target, targetLane);
        }

        private StageLane DetermineTargetLane(Vehicle vehicle)
        {
            var stage = vehicle.CurrentStage;
            int targetIndex;

            if (useRelativeOffset)
            {
                // Relative: Add offset to current lane index
                int currentIndex = stage.GetLaneIndex(vehicle.CurrentLane);
                if (currentIndex < 0)
                {
                    Debug.LogWarning($"[LaneChangeEffect] Current lane of '{vehicle.name}' was not found in stage '{stage.name}'.");
                    return null;
                }

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