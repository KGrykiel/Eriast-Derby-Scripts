using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts.Stages.Lanes;
using Assets.Scripts.StatusEffects;
using Assets.Scripts.Combat.SkillChecks;
using Assets.Scripts.Combat.Saves;
using Assets.Scripts.Effects;
using EventCard = Assets.Scripts.Events.EventCard.EventCard;

namespace Assets.Scripts.Stages
{
    public class Stage : MonoBehaviour
    {
        // ==================== CONFIGURATION ====================
    
        [Header("Stage Identity")]
        public string stageName;
    
        [Tooltip("Distance to traverse this stage (D&D-style discrete units)")]
        public int length = 100;
    
        [Tooltip("Possible next stages after completing this one")]
        public List<Stage> nextStages = new();
    
        [Tooltip("Is this stage a finish line?")]
        public bool isFinishLine = false;
    
        [Header("Stage Effects")]
        [Tooltip("StatusEffect applied to all vehicle components when entering stage (e.g., sandstorm, freezing winds)")]
        public StatusEffect stageStatusEffect;
    
        [Header("Event Cards")]
        [Tooltip("Random events that can occur in this stage")]
        public List<EventCard> eventCards = new();
    
        [Header("Lane System")]
        [Tooltip("Lanes in this stage - auto-populated from child StageLane components")]
        public List<StageLane> lanes = new();
    
        // ==================== RUNTIME DATA ====================
    
        [HideInInspector]
        public List<Vehicle> vehiclesInStage = new();
    
        // ==================== UNITY LIFECYCLE ====================
    
        private void Awake()
        {
            // Auto-discover lanes from children (similar to Vehicle component discovery)
            DiscoverLanes();
        }

        /// <summary>
        /// Gizmos for visualisation. 
        /// </summary>
        private void OnDrawGizmos()
        {
            if (nextStages != null)
            {
                Gizmos.color = Color.cyan;
                foreach (var nextStage in nextStages)
                {
                    if (nextStage != null)
                    {
                        Gizmos.DrawLine(transform.position, nextStage.transform.position);
                    }
                }
            }
        }
    
        // ==================== STAGE ENTRY/EXIT ====================
        /// <summary>
        /// When a vehicle enters the stage.
        /// </summary>
        public void TriggerEnter(Vehicle vehicle)
        {
            if (vehicle == null) return;

            if (!vehiclesInStage.Contains(vehicle))
                vehiclesInStage.Add(vehicle);

            if (stageStatusEffect != null)
                ApplyStageStatusEffect(vehicle);

            if (lanes != null && lanes.Count > 0)
                AssignVehicleToEntryLane(vehicle);

            DrawAndTriggerEventCard(vehicle);
        }

        /// <summary>
        /// When a vehicle leaves the stage.
        /// </summary>
        public void TriggerLeave(Vehicle vehicle)
        {
            if (vehicle == null) return;

            bool wasPresent = vehiclesInStage.Remove(vehicle);

            if (wasPresent)
            {
                vehicle.previousStage = this;

                if (stageStatusEffect != null)
                    RemoveStageStatusEffect(vehicle);

                StageLane currentLane = GetVehicleLane(vehicle);
                if (currentLane != null)
                {
                    if (currentLane.laneStatusEffect != null)
                    {
                        RemoveLaneStatusEffect(vehicle, currentLane);
                    }
                
                    currentLane.vehiclesInLane.Remove(vehicle);
                }

                this.LogStageExit(vehicle, vehiclesInStage.Count);
            }
        }
    
        // ==================== STAGE STATUS EFFECTS ====================
    
        private void ApplyStageStatusEffect(Vehicle vehicle)
        {
            foreach (var component in vehicle.AllComponents)
            {
                if (component != null)
                {
                    component.ApplyStatusEffect(stageStatusEffect, this);
                }
            }
        }
    
        private void RemoveStageStatusEffect(Vehicle vehicle)
        {
            foreach (var component in vehicle.AllComponents)
            {
                if (component != null)
                {
                    component.RemoveStatusEffectsFromSource(this);
                }
            }
        }

            // ==================== EVENT CARD SYSTEM ====================

            /// <summary>
            /// Logic for drawing a random card from the stage's event card list and triggering its effects on the vehicle.
            /// Will probably want to add conditions to the cards instead of being completely random.
            /// TODO: design work needed.
            /// </summary>
            public void DrawAndTriggerEventCard(Vehicle vehicle)
        {
            if (eventCards == null || eventCards.Count == 0) return;

            var card = eventCards[Random.Range(0, eventCards.Count)];
            this.LogEventCardTrigger(vehicle, card.name);
            card.Trigger(vehicle);
        }
    
        // ==================== LANE SYSTEM ====================
    
        private void DiscoverLanes()
        {
            lanes.Clear();

            foreach (Transform child in transform)
            {
                if (child.TryGetComponent<StageLane>(out var lane))
                    lanes.Add(lane);
            }

            lanes.Sort((a, b) => a.transform.GetSiblingIndex().CompareTo(b.transform.GetSiblingIndex()));
        }

        public int GetLaneIndex(StageLane lane)
        {
            if (lane == null || lanes == null) return -1;
            return lanes.IndexOf(lane);
        }

        public StageLane GetLaneByIndex(int index)
        {
            if (lanes == null || index < 0 || index >= lanes.Count) return null;
            return lanes[index];
        }

        public StageLane GetVehicleLane(Vehicle vehicle)
        {
            if (vehicle == null || lanes == null || lanes.Count == 0)
                return null;

            if (vehicle.currentLane != null && lanes.Contains(vehicle.currentLane))
                return vehicle.currentLane;

            foreach (var lane in lanes)
            {
                if (lane.vehiclesInLane.Contains(vehicle))
                    return lane;
            }

            return null;
        }
    
        public void AssignVehicleToLane(Vehicle vehicle, StageLane targetLane)
        {
            if (vehicle == null || targetLane == null)
                return;

            if (!lanes.Contains(targetLane))
            {
                Debug.LogWarning($"Stage.AssignVehicleToLane: Lane {targetLane.laneName} does not belong to stage {stageName}");
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
            if (vehicle == null || lanes == null || lanes.Count == 0)
                return;

            int defaultLaneIndex = lanes.Count / 2;
            AssignVehicleToLane(vehicle, lanes[defaultLaneIndex]);
        }
    
        private void AssignVehicleToEntryLane(Vehicle vehicle)
        {
            if (vehicle == null || lanes == null || lanes.Count == 0)
                return;

            StageLane targetLane = null;

            if (vehicle.previousStage != null && vehicle.currentLane != null)
            {
                var previousLane = vehicle.currentLane;

                if (previousLane.targetLaneIndex >= 0 && previousLane.targetLaneIndex < lanes.Count)
                    targetLane = lanes[previousLane.targetLaneIndex];
                else
                    targetLane = GetProportionalLane(vehicle.previousStage, previousLane);
            }

            if (targetLane == null)
            {
                int defaultLaneIndex = lanes.Count / 2;
                targetLane = lanes[defaultLaneIndex];
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

            int newLaneIndex = Mathf.RoundToInt(positionRatio * (lanes.Count - 1));
            newLaneIndex = Mathf.Clamp(newLaneIndex, 0, lanes.Count - 1);

            return lanes[newLaneIndex];
        }
    
        private void ApplyLaneStatusEffect(Vehicle vehicle, StatusEffect laneEffect, StageLane lane)
        {
            foreach (var component in vehicle.AllComponents)
            {
                if (component != null)
                    component.ApplyStatusEffect(laneEffect, lane);
            }
        }
    
        private void RemoveLaneStatusEffect(Vehicle vehicle, StageLane lane)
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
                    this.LogLaneTurnEffect(vehicle, lane, effect, true);
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
                    vehicle, effect.checkSpec, effect.dc, causalSource: this);

                if (result.Roll.Success)
                {
                    ApplyTurnEffects(vehicle, effect.onSuccess);
                }
                else
                {
                    ApplyTurnEffects(vehicle, effect.onFailure);
                }

                this.LogLaneTurnEffectWithCheck(vehicle, lane, effect, result);
            }
    
        private void ResolveSavingThrowTurnEffect(Vehicle vehicle, StageLane lane, LaneTurnEffect effect)
            {
                var result = SavePerformer.Execute(
                    vehicle, effect.saveSpec, effect.dc, causalSource: this);

                if (result.Roll.Success)
                {
                    ApplyTurnEffects(vehicle, effect.onSuccess);
                }
                else
                {
                    ApplyTurnEffects(vehicle, effect.onFailure);
                }

                this.LogLaneTurnEffectWithSave(vehicle, lane, effect, result);
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
                        vehicle.chassis,
                        new EffectContext(),
                        this
                    );
                }
            }
        }
    }
}




