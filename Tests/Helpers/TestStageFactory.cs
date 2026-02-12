using UnityEngine;
using Assets.Scripts.Stages;
using Assets.Scripts.Stages.Lanes;
using Assets.Scripts.StatusEffects;

namespace Assets.Scripts.Tests.Helpers
{
    /// <summary>
    /// Factory for creating Stage and Lane objects in tests.
    /// </summary>
    public static class TestStageFactory
    {
        /// <summary>
        /// Create a basic Stage with empty vehicle and lane lists.
        /// </summary>
        /// <param name="name">Stage name</param>
        /// <param name="stageObject">Output: created GameObject (for cleanup)</param>
        public static Stage CreateStage(string name, out GameObject stageObject)
        {
            stageObject = new GameObject(name);
            var stage = stageObject.AddComponent<Stage>();
            stage.stageName = name;
            stage.vehiclesInStage = new System.Collections.Generic.List<Vehicle>();
            stage.lanes = new System.Collections.Generic.List<StageLane>();
            return stage;
        }

        /// <summary>
        /// Create a StageLane as a child of a Stage.
        /// </summary>
        /// <param name="name">Lane name</param>
        /// <param name="stage">Parent stage</param>
        /// <param name="laneStatusEffect">Optional status effect applied to vehicles in this lane</param>
        /// <param name="stageObject">Parent GameObject (needed for hierarchy)</param>
        public static StageLane CreateLane(
            string name,
            Stage stage,
            GameObject stageObject,
            StatusEffect laneStatusEffect = null)
        {
            var laneObj = new GameObject(name);
            laneObj.transform.SetParent(stageObject.transform);
            var lane = laneObj.AddComponent<StageLane>();
            lane.laneName = name;
            lane.vehiclesInLane = new System.Collections.Generic.List<Vehicle>();
            lane.turnEffects = new System.Collections.Generic.List<LaneTurnEffect>();
            lane.laneStatusEffect = laneStatusEffect;
            stage.lanes.Add(lane);
            return lane;
        }

        /// <summary>
        /// Create a LaneTurnEffect for testing lane hazards.
        /// </summary>
        /// <param name="name">Effect name</param>
        /// <param name="checkType">Type of check required</param>
        /// <param name="dc">Difficulty class</param>
        public static LaneTurnEffect CreateLaneTurnEffect(
            string name,
            LaneCheckType checkType,
            int dc = 15)
        {
            return new LaneTurnEffect
            {
                effectName = name,
                description = $"Test hazard: {name}",
                checkType = checkType,
                dc = dc,
                onSuccess = new System.Collections.Generic.List<EffectInvocation>(),
                onFailure = new System.Collections.Generic.List<EffectInvocation>()
            };
        }
    }
}
