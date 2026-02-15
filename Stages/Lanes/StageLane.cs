using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts.StatusEffects;

namespace Assets.Scripts.Stages.Lanes
{
    /// <summary>
    /// Lane system for stage design. Used to give a stage its identity and unique feel.
    /// Moreover made to stimulate interesting player choices and tradeoffs in lane selection prompting discussion and cooperation.
    /// </summary>
    public class StageLane : MonoBehaviour
    {
        // ==================== IDENTITY ====================
        
        [Header("Identity")]
        [Tooltip("Display name for this lane (e.g., 'Cliff Edge', 'Main Road')")]
        public string laneName = "Lane";

        // ==================== MODIFIERS ====================
        
        [Header("Lane Modifiers")]
        [Tooltip("StatusEffect applied to ALL vehicle components while in this lane.\n")]
        public StatusEffect laneStatusEffect;
        
        // ==================== TURN EFFECTS ====================
        
        [Header("Every Turn Effects")]
        [Tooltip("Effects that trigger every turn for vehicles in this lane")]
        public List<LaneTurnEffect> turnEffects = new();
        
        // ==================== STAGE TRANSITION ====================
        
        [Header("Stage Transition")]
        [Tooltip("Which stage this lane leads to (null = use Stage.nextStages default)")]
        public Stage nextStage;
        
        [Tooltip("Which lane in the target stage to enter (-1 = use proportional mapping)")]
        [Range(-1, 10)]
        public int targetLaneIndex = -1;
        
        // ==================== RUNTIME DATA ====================
        
        [HideInInspector]
        public List<Vehicle> vehiclesInLane = new();
    }
}
