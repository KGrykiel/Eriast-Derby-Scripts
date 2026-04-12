using System.Collections.Generic;
using Assets.Scripts.Entities.Vehicles;
using Assets.Scripts.Stages;
using Assets.Scripts.Stages.Lanes;
using UnityEngine;

namespace Assets.Scripts.Managers
{
    /// <summary>
    /// Handles vehicle grid placement at race start.
    /// Owns the strategy for distributing vehicles across starting lanes,
    /// keeping that policy out of Stage and LaneManager.
    /// </summary>
    public static class RaceInitialiser
    {
        /// <summary>
        /// Distributes vehicles across the starting stage's lanes in round-robin order
        /// and registers each with the stage.
        /// </summary>
        public static void PlaceVehicles(Stage startStage, List<Vehicle> vehicles)
        {
            if (startStage == null || vehicles == null) return;

            List<StageLane> lanes = startStage.lanes;
            if (lanes == null || lanes.Count == 0)
            {
                Debug.LogWarning("[RaceInitialiser] Start stage has no lanes.");
                return;
            }

            for (int i = 0; i < vehicles.Count; i++)
            {
                Vehicle vehicle = vehicles[i];
                StageLane targetLane = lanes[i % lanes.Count];
                RacePositionTracker.SetStage(vehicle, startStage);
                RacePositionTracker.SetProgress(vehicle, 0);
                startStage.TriggerEnter(vehicle, targetLane);
            }
        }
    }
}
