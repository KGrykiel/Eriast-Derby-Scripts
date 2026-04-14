using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts.Managers;
using Assets.Scripts.Stages.Lanes;
using Assets.Scripts.Combat.Rolls.RollSpecs;
using Assets.Scripts.Entities.Vehicles;

namespace Assets.Scripts.Stages
{
    /// <summary>
    /// Manages lane assignment, lane status effects, and lane turn effect resolution for a Stage.
    /// Extracted from Stage to separate lane-specific logic from core stage logic.
    /// </summary>
    public class LaneManager
    {
        private readonly Stage stage;
        private List<StageLane> Lanes => stage.lanes;

        public LaneManager(Stage stage)
        {
            this.stage = stage;
        }

        // ==================== LANE DISCOVERY ====================

        public void DiscoverLanes()
        {
            // Append any child StageLanes not already in the list, preserving inspector-assigned order
            foreach (Transform child in stage.transform)
            {
                if (child.TryGetComponent<StageLane>(out var lane) && !Lanes.Contains(lane))
                    Lanes.Add(lane);
            }
        }

        // ==================== LANE QUERIES ====================

        public int GetLaneIndex(StageLane lane)
        {
            if (lane == null || Lanes == null) return -1;
            return Lanes.IndexOf(lane);
        }

        public StageLane GetLaneByIndex(int index)
        {
            if (Lanes == null || index < 0 || index >= Lanes.Count) return null;
            return Lanes[index];
        }

        // ==================== LANE ASSIGNMENT ====================

        public void AssignIncomingVehicle(Vehicle vehicle, StageLane targetLane = null)
        {
            if (targetLane != null)
                AssignVehicleToLane(vehicle, targetLane);
        }

        public void AssignVehicleToLane(Vehicle vehicle, StageLane targetLane)
        {
            if (vehicle == null || targetLane == null) return;
            if (!Lanes.Contains(targetLane))
            {
                Debug.LogWarning($"LaneManager.AssignVehicleToLane: Lane {targetLane.laneName} does not belong to stage {stage.stageName}");
                return;
            }

            StageLane currentLane = RacePositionTracker.GetLane(vehicle);
            if (currentLane != null && currentLane != targetLane)
                ExecuteRollNodes(currentLane.onExitEffects, vehicle, currentLane.name);

            RacePositionTracker.SetLane(vehicle, targetLane);
            ExecuteRollNodes(targetLane.onEnterEffects, vehicle, targetLane.name);
        }

        public void HandleStageExit(Vehicle vehicle)
        {
            StageLane lane = RacePositionTracker.GetLane(vehicle);
            if (lane == null) return;
            ExecuteRollNodes(lane.onExitEffects, vehicle, lane.name);
            RacePositionTracker.SetLane(vehicle, null);
        }

        private static void ExecuteRollNodes(List<RollNode> nodes, Vehicle vehicle, string source)
        {
            if (nodes == null || nodes.Count == 0) return;
            foreach (var rollNode in nodes)
            {
                if (rollNode == null) continue;
                var ctx = new RollContext { Target = vehicle, CausalSource = source };
                RollNodeExecutor.Execute(rollNode, ctx);
            }
        }

        // ==================== LANE TURN EFFECTS ====================

        public void ProcessLaneTurnEffects(Vehicle vehicle)
        {
            if (vehicle == null) return;

            StageLane currentLane = RacePositionTracker.GetLane(vehicle);
            if (currentLane == null || currentLane.turnEffects == null)
                return;

            foreach (var rollNode in currentLane.turnEffects)
            {
                if (rollNode == null) continue;
                var ctx = new RollContext { Target = vehicle, CausalSource = stage.name };
                bool success = RollNodeExecutor.Execute(rollNode, ctx);
                stage.LogLaneTurnEffect(vehicle, currentLane, success);
            }
        }
    }
}
