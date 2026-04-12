using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts.Stages.Lanes;
using Assets.Scripts.Conditions;
using Assets.Scripts.Combat.Rolls.RollSpecs;
using EventCard = Assets.Scripts.Events.EventCard.EventCard;
using Assets.Scripts.Entities.Vehicles;
using Assets.Scripts.Managers;

namespace Assets.Scripts.Stages
{
    public class Stage : MonoBehaviour
    {
        // ==================== CONFIGURATION ====================

        [Header("Stage Identity")]
        public string stageName;

        [Tooltip("Distance to traverse this stage (D&D-style discrete units)")]
        public int length = 100;

        [Header("Stage Effects")]
        [Tooltip("Effects executed when a vehicle enters this stage.")]
        public List<RollNode> onEnterEffects = new();

        [Tooltip("Effects executed when a vehicle exits this stage.")]
        public List<RollNode> onExitEffects = new();

        [Tooltip("Effects that trigger every turn for vehicles in this stage.")]
        public List<RollNode> turnEffects = new();

        [Header("Event Cards")]
        [Tooltip("Random events that can occur in this stage")]
        public List<EventCard> eventCards = new();

        [Header("Lane System")]
        [Tooltip("Lanes in this stage - auto-populated from child StageLane components")]
        public List<StageLane> lanes = new();

        // ==================== RUNTIME DATA ====================

        private LaneManager laneManager;

        // ==================== UNITY LIFECYCLE ====================

        private void OnEnable() => TrackDefinition.Register(this);
        private void OnDisable() => TrackDefinition.Unregister(this);

        private void Awake()
        {
            laneManager = new LaneManager(this);
            // Auto-discover lanes from children (similar to Vehicle component discovery)
            laneManager.DiscoverLanes();
        }

        // ==================== TRIGGERS ====================

        public void TriggerEnter(Vehicle vehicle, StageLane targetLane = null)
        {
            if (vehicle == null) return;

            foreach (var rollNode in onEnterEffects)
            {
                if (rollNode == null) continue;
                var ctx = new RollContext { Target = vehicle, CausalSource = name };
                RollNodeExecutor.Execute(rollNode, ctx);
            }

            laneManager.AssignIncomingVehicle(vehicle, targetLane);
            DrawAndTriggerEventCard(vehicle);
        }

        public void TriggerLeave(Vehicle vehicle)
        {
            if (vehicle == null) return;

            bool wasPresent = RacePositionTracker.GetStage(vehicle) == this;

            if (wasPresent)
            {
                laneManager.HandleStageExit(vehicle);

                foreach (var rollNode in onExitEffects)
                {
                    if (rollNode == null) continue;
                    var ctx = new RollContext { Target = vehicle, CausalSource = name };
                    RollNodeExecutor.Execute(rollNode, ctx);
                }

                vehicle.NotifyStatusEffectTrigger(RemovalTrigger.OnStageExit);
                this.LogStageExit(vehicle, RacePositionTracker.GetVehiclesInStage(this).Count - 1);
            }
        }

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

        public void ProcessStageTurnEffects(Vehicle vehicle)
        {
            if (vehicle == null) return;
            foreach (var rollNode in turnEffects)
            {
                if (rollNode == null) continue;
                var ctx = new RollContext { Target = vehicle, CausalSource = name };
                bool success = RollNodeExecutor.Execute(rollNode, ctx);
                this.LogStageTurnEffect(vehicle, success);
            }
        }

        // ==================== STAGE GRAPH ====================

        public IEnumerable<Stage> GetConnectedStages() => TrackDefinition.GetConnected(this);

        // ==================== LANE SYSTEM (delegated to LaneManager) ====================

        public int GetLaneIndex(StageLane lane) => laneManager.GetLaneIndex(lane);
        public StageLane GetLaneByIndex(int index) => laneManager.GetLaneByIndex(index);
        public void AssignVehicleToLane(Vehicle vehicle, StageLane targetLane) => laneManager.AssignVehicleToLane(vehicle, targetLane);
        public void ProcessLaneTurnEffects(Vehicle vehicle) => laneManager.ProcessLaneTurnEffects(vehicle);
    }
}


