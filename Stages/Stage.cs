using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts.Stages.Lanes;
using Assets.Scripts.StatusEffects;
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

        private LaneManager laneManager;

        // ==================== UNITY LIFECYCLE ====================

        private void Awake()
        {
            laneManager = new LaneManager(this);
            // Auto-discover lanes from children (similar to Vehicle component discovery)
            laneManager.DiscoverLanes();
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
                laneManager.AssignVehicleToEntryLane(vehicle);

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

                StageLane currentLane = laneManager.GetVehicleLane(vehicle);
                if (currentLane != null)
                {
                    if (currentLane.laneStatusEffect != null)
                        laneManager.RemoveLaneStatusEffect(vehicle, currentLane);

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
                    component.ApplyStatusEffect(stageStatusEffect, this);
            }
        }

        private void RemoveStageStatusEffect(Vehicle vehicle)
        {
            foreach (var component in vehicle.AllComponents)
            {
                if (component != null)
                    component.RemoveStatusEffectsFromSource(this);
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

        // ==================== LANE SYSTEM (delegated to LaneManager) ====================

        public int GetLaneIndex(StageLane lane) => laneManager.GetLaneIndex(lane);
        public StageLane GetLaneByIndex(int index) => laneManager.GetLaneByIndex(index);
        public StageLane GetVehicleLane(Vehicle vehicle) => laneManager.GetVehicleLane(vehicle);
        public void AssignVehicleToLane(Vehicle vehicle, StageLane targetLane) => laneManager.AssignVehicleToLane(vehicle, targetLane);
        public void AssignVehicleToDefaultLane(Vehicle vehicle) => laneManager.AssignVehicleToDefaultLane(vehicle);
        public void ProcessLaneTurnEffects(Vehicle vehicle) => laneManager.ProcessLaneTurnEffects(vehicle);
    }
}


