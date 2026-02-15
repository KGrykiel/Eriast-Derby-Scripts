using UnityEngine;
using Assets.Scripts.Stages.Lanes;
using Assets.Scripts.Effects;

/// <summary>Effect for vehicle changing lanes. Pass chassis as the vehicle</summary>
[System.Serializable]
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

    public override void Apply(Entity target, EffectContext context, Object source = null)
    {
        // Get vehicle (from target or parent)
        Vehicle vehicle = EntityHelpers.GetParentVehicle(target);
        if (vehicle == null || vehicle.currentStage == null)
        {
            return; // Silent fail - caller should validate before applying
        }
            
        // Determine target lane
        StageLane targetLane = DetermineTargetLane(vehicle);
        if (targetLane == null)
        {
            return; // Silent fail - invalid lane
        }
            
        // Execute lane change via Stage (handles StatusEffect application/removal)
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
