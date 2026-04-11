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

        [Tooltip("Is this stage a finish line?")]
        public bool isFinishLine = false;

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

        [HideInInspector]
        public List<Vehicle> vehiclesInStage = new();

        private LaneManager laneManager;

        // ==================== UNITY LIFECYCLE ====================

        private void OnEnable() => StageRegistry.Register(this);
        private void OnDisable() => StageRegistry.Unregister(this);

        private void Awake()
        {
            laneManager = new LaneManager(this);
            // Auto-discover lanes from children (similar to Vehicle component discovery)
            laneManager.DiscoverLanes();
        }

        private void Start()
        {
            // WireStageLinks stores references to prefab assets, not scene instances.
            // Re-resolve them here so nextStage/nextStages point at live objects with
            // initialised laneManagers. All stages are registered by OnEnable, which
            // runs before any Start, so the registry is fully populated at this point.
            ResolveStageLinks();
        }

        private void ResolveStageLinks()
        {
            foreach (var lane in lanes)
            {
                if (lane == null || lane.nextLane == null) continue;
                Stage targetStageAsset = lane.nextLane.GetComponentInParent<Stage>();
                if (targetStageAsset == null) continue;
                Stage targetStageInstance = StageRegistry.FindByName(targetStageAsset.stageName);
                if (targetStageInstance == null || targetStageInstance == targetStageAsset) continue;
                StageLane resolved = targetStageInstance.lanes.Find(l => l != null && l.laneName == lane.nextLane.laneName);
                if (resolved != null)
                    lane.nextLane = resolved;
            }
        }

        // ==================== TRIGGERS ====================

        public void TriggerEnter(Vehicle vehicle, StageLane targetLane = null)
        {
            if (vehicle == null) return;

            if (!vehiclesInStage.Contains(vehicle))
                vehiclesInStage.Add(vehicle);

            foreach (var rollNode in onEnterEffects)
            {
                if (rollNode == null) continue;
                var ctx = new RollContext { Target = vehicle, CausalSource = name };
                RollNodeExecutor.Execute(rollNode, ctx);
            }

            DrawAndTriggerEventCard(vehicle);

            laneManager.AssignIncomingVehicle(vehicle, targetLane);
        }

        public void TriggerLeave(Vehicle vehicle)
        {
            if (vehicle == null) return;

            bool wasPresent = vehiclesInStage.Remove(vehicle);

            if (wasPresent)
            {
                vehicle.SetPreviousStage(this);
                laneManager.HandleStageExit(vehicle);

                foreach (var rollNode in onExitEffects)
                {
                    if (rollNode == null) continue;
                    var ctx = new RollContext { Target = vehicle, CausalSource = name };
                    RollNodeExecutor.Execute(rollNode, ctx);
                }

                vehicle.NotifyStatusEffectTrigger(RemovalTrigger.OnStageExit);
                this.LogStageExit(vehicle, vehiclesInStage.Count);
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

        public IEnumerable<Stage> GetConnectedStages()
        {
            var seen = new HashSet<Stage>();
            foreach (var lane in lanes)
            {
                if (lane == null || lane.nextLane == null) continue;
                Stage next = lane.nextLane.GetComponentInParent<Stage>();
                if (next != null && seen.Add(next))
                    yield return next;
            }
        }

        // ==================== LANE SYSTEM (delegated to LaneManager) ====================

        public int GetLaneIndex(StageLane lane) => laneManager.GetLaneIndex(lane);
        public StageLane GetLaneByIndex(int index) => laneManager.GetLaneByIndex(index);
        public void AssignVehicleToLane(Vehicle vehicle, StageLane targetLane) => laneManager.AssignVehicleToLane(vehicle, targetLane);
        public void ProcessLaneTurnEffects(Vehicle vehicle) => laneManager.ProcessLaneTurnEffects(vehicle);
    }
}


