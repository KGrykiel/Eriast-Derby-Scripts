using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts.Combat.Rolls.RollSpecs;
using Assets.Scripts.Entities;
using Assets.Scripts.Entities.Vehicles;

namespace Assets.Scripts.Stages.Lanes
{
    /// <summary>
    /// Lane system for stage design. Used to give a stage its identity and unique feel.
    /// Moreover made to stimulate interesting player choices and tradeoffs in lane selection prompting discussion and cooperation.
    /// </summary>
    public class StageLane : MonoBehaviour, IRollTarget
    {
        // ==================== IDENTITY ====================
        
        [Header("Identity")]
        [Tooltip("Display name for this lane (e.g., 'Cliff Edge', 'Main Road')")]
        public string laneName = "Lane";

        // ==================== LANE EFFECTS ====================

        [Header("Lane Effects")]
        [Tooltip("Effects executed when a vehicle enters this lane.")]
        public List<RollNode> onEnterEffects = new();

        [Tooltip("Effects executed when a vehicle exits this lane.")]
        public List<RollNode> onExitEffects = new();

        // ==================== TURN EFFECTS ====================

        [Header("Every Turn Effects")]
        [Tooltip("Effects that trigger every turn for vehicles in this lane")]
        public List<RollNode> turnEffects = new();
        
        // ==================== STAGE TRANSITION ====================

        [Header("Stage Transition")]
        [Tooltip("The lane in the next stage this lane leads to.")]
        public StageLane nextLane;
        
        // ==================== RUNTIME DATA ====================

        [HideInInspector]
        public List<Vehicle> vehiclesInLane = new();
    }
}
