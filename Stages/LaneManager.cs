using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts.Stages.Lanes;
using Assets.Scripts.StatusEffects;
using Assets.Scripts.Combat.SkillChecks;
using Assets.Scripts.Combat.Saves;
using Assets.Scripts.Effects;

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
            Lanes.Clear();

            foreach (Transform child in stage.transform)
            {
                if (child.TryGetComponent<StageLane>(out var lane))
                    Lanes.Add(lane);
            }

            Lanes.Sort((a, b) => a.transform.GetSiblingIndex().CompareTo(b.transform.GetSiblingIndex()));
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

        public StageLane GetVehicleLane(Vehicle vehicle)
        {
            if (vehicle == null || Lanes == null || Lanes.Count == 0)
                return null;

            if (vehicle.currentLane != null && Lanes.Contains(vehicle.currentLane))
                return vehicle.currentLane;

            foreach (var lane in Lanes)
            {
                if (lane.vehiclesInLane.Contains(vehicle))
                    return lane;
            }

            return null;
        }

        // ==================== LANE ASSIGNMENT ====================

        public void AssignVehicleToLane(Vehicle vehicle, StageLane targetLane)
        {
            if (vehicle == null || targetLane == null)
                return;

            if (!Lanes.Contains(targetLane))
            {
                Debug.LogWarning($"LaneManager.AssignVehicleToLane: Lane {targetLane.laneName} does not belong to stage {stage.stageName}");
                return;
            }

            StageLane currentLane = GetVehicleLane(vehicle);

            if (currentLane != null && currentLane != targetLane && currentLane.laneStatusEffect != null)
                RemoveLaneStatusEffect(vehicle, currentLane);

            if (currentLane != null && currentLane != targetLane)
                currentLane.vehiclesInLane.Remove(vehicle);

            if (!targetLane.vehiclesInLane.Contains(vehicle))
                targetLane.vehiclesInLane.Add(vehicle);

            vehicle.currentLane = targetLane;

            if (targetLane.laneStatusEffect != null)
                ApplyLaneStatusEffect(vehicle, targetLane.laneStatusEffect, targetLane);
        }

        public void AssignVehicleToDefaultLane(Vehicle vehicle)
        {
            if (vehicle == null || Lanes == null || Lanes.Count == 0)
                return;

            int defaultLaneIndex = Lanes.Count / 2;
            AssignVehicleToLane(vehicle, Lanes[defaultLaneIndex]);
        }

        public void AssignVehicleToEntryLane(Vehicle vehicle)
        {
            if (vehicle == null || Lanes == null || Lanes.Count == 0)
                return;

            StageLane targetLane = null;

            if (vehicle.previousStage != null && vehicle.currentLane != null)
            {
                var previousLane = vehicle.currentLane;

                if (previousLane.targetLaneIndex >= 0 && previousLane.targetLaneIndex < Lanes.Count)
                    targetLane = Lanes[previousLane.targetLaneIndex];
                else
                    targetLane = GetProportionalLane(vehicle.previousStage, previousLane);
            }

            if (targetLane == null)
            {
                int defaultLaneIndex = Lanes.Count / 2;
                targetLane = Lanes[defaultLaneIndex];
            }

            AssignVehicleToLane(vehicle, targetLane);
        }

        /// <summary>Maps lane position proportionally between stages (left stays left, right stays right).</summary>
        private StageLane GetProportionalLane(Stage previousStage, StageLane previousLane)
        {
            if (previousStage == null || previousLane == null)
                return null;

            int oldLaneIndex = previousStage.GetLaneIndex(previousLane);
            int oldLaneCount = previousStage.lanes.Count;

            if (oldLaneIndex < 0 || oldLaneCount <= 0)
                return null;

            float positionRatio = oldLaneCount > 1 
                ? oldLaneIndex / (float)(oldLaneCount - 1) 
                : 0.5f;

            int newLaneIndex = Mathf.RoundToInt(positionRatio * (Lanes.Count - 1));
            newLaneIndex = Mathf.Clamp(newLaneIndex, 0, Lanes.Count - 1);

            return Lanes[newLaneIndex];
        }

        // ==================== LANE STATUS EFFECTS ====================

        private void ApplyLaneStatusEffect(Vehicle vehicle, StatusEffect laneEffect, StageLane lane)
        {
            foreach (var component in vehicle.AllComponents)
            {
                if (component != null)
                    component.ApplyStatusEffect(laneEffect, lane);
            }
        }

        public void RemoveLaneStatusEffect(Vehicle vehicle, StageLane lane)
        {
            if (lane == null) return;

            foreach (var component in vehicle.AllComponents)
            {
                if (component != null)
                    component.RemoveStatusEffectsFromSource(lane);
            }
        }

        // ==================== LANE TURN EFFECTS ====================

        public void ProcessLaneTurnEffects(Vehicle vehicle)
        {
            if (vehicle == null) return;

            StageLane currentLane = GetVehicleLane(vehicle);
            if (currentLane == null || currentLane.turnEffects == null || currentLane.turnEffects.Count == 0)
                return;

            foreach (var turnEffect in currentLane.turnEffects)
            {
                if (turnEffect == null) continue;
                ResolveLaneTurnEffect(vehicle, currentLane, turnEffect);
            }
        }

        private void ResolveLaneTurnEffect(Vehicle vehicle, StageLane lane, LaneTurnEffect effect)
        {
            switch (effect.checkType)
            {
                case LaneCheckType.None:
                    ApplyTurnEffects(vehicle, effect.onSuccess);
                    stage.LogLaneTurnEffect(vehicle, lane, effect, true);
                    break;

                case LaneCheckType.SkillCheck:
                    ResolveSkillCheckTurnEffect(vehicle, lane, effect);
                    break;

                case LaneCheckType.SavingThrow:
                    ResolveSavingThrowTurnEffect(vehicle, lane, effect);
                    break;
            }
        }

        private void ResolveSkillCheckTurnEffect(Vehicle vehicle, StageLane lane, LaneTurnEffect effect)
        {
            var result = SkillCheckPerformer.Execute(
                vehicle, effect.checkSpec, effect.dc, causalSource: stage);

            if (result.Roll.Success)
                ApplyTurnEffects(vehicle, effect.onSuccess);
            else
                ApplyTurnEffects(vehicle, effect.onFailure);

            stage.LogLaneTurnEffectWithCheck(vehicle, lane, effect, result);
        }

        private void ResolveSavingThrowTurnEffect(Vehicle vehicle, StageLane lane, LaneTurnEffect effect)
        {
            var result = SavePerformer.Execute(
                vehicle, effect.saveSpec, effect.dc, causalSource: stage);

            if (result.Roll.Success)
                ApplyTurnEffects(vehicle, effect.onSuccess);
            else
                ApplyTurnEffects(vehicle, effect.onFailure);

            stage.LogLaneTurnEffectWithSave(vehicle, lane, effect, result);
        }

        private void ApplyTurnEffects(Vehicle vehicle, List<EffectInvocation> effects)
        {
            if (effects == null || effects.Count == 0) return;

            foreach (var effectInvocation in effects)
            {
                if (effectInvocation?.effect != null)
                {
                    effectInvocation.effect.Apply(
                        vehicle.chassis,
                        new EffectContext(),
                        stage
                    );
                }
            }
        }
    }
}
