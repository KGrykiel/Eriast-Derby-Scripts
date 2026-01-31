using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts.StatusEffects;

namespace Assets.Scripts.Stages.Lanes
{
    /// <summary>
    /// Represents a lane within a stage.
    /// Lanes provide tactical positioning - different lanes have different modifiers
    /// that affect speed, defense, and role-specific checks.
    /// 
    /// Design: MonoBehaviour attached to child GameObjects under Stage.
    /// Stage auto-discovers lanes from children, similar to how Vehicle discovers components.
    /// Lane GameObject is used as the source for StatusEffects, enabling efficient removal.
    /// 
    /// Modifiers are applied via StatusEffect that gets applied to ALL vehicle components.
    /// Each component uses only the modifiers relevant to it (AC for all, MaxSpeed for drive, etc.)
    /// </summary>
    public class StageLane : MonoBehaviour
    {
        // ==================== IDENTITY ====================
        
        [Header("Identity")]
        [Tooltip("Display name for this lane (e.g., 'Cliff Edge', 'Main Road')")]
        public string laneName = "Lane";
        
        // NOTE: laneIndex is NOT stored here - it's determined by position in Stage.lanes list
        // Use Stage.GetLaneIndex(lane) to get the index if needed
        
        // ==================== MODIFIERS ====================
        
        [Header("Lane Modifiers")]
        [Tooltip("StatusEffect applied to ALL vehicle components while in this lane.\n" +
                 "Each component uses only relevant modifiers:\n" +
                 "- AC modifier affects all components\n" +
                 "- MaxSpeed affects drive\n" +
                 "- EnergyRegeneration affects power core\n" +
                 "Example: 'Cliff Edge' could provide -2 AC (exposed) but +5 MaxSpeed (downhill)")]
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
        
        /// <summary>
        /// List of vehicles currently in this lane.
        /// Managed by Stage.AssignVehicleToLane() and LaneChangeEffect.
        /// </summary>
        [HideInInspector]
        public List<Vehicle> vehiclesInLane = new();
        
        // ==================== HELPER METHODS ====================
        
        /// <summary>
        /// Can a vehicle enter this lane?
        /// Light system: Always returns true
        /// Heavy system (future): Would check capacity and state
        /// </summary>
        public bool CanEnter()
        {
            // Light system: All lanes always open
            return true;
        }
        
        /// <summary>
        /// Get a summary string for tooltips/UI
        /// </summary>
        public string GetSummary()
        {
            var parts = new List<string>();
            
            // Add status effect name if present
            if (laneStatusEffect != null)
            {
                parts.Add(laneStatusEffect.effectName);
            }
            
            // Add turn effects summary
            if (turnEffects != null && turnEffects.Count > 0)
            {
                foreach (var effect in turnEffects)
                {
                    if (!string.IsNullOrEmpty(effect.effectName))
                        parts.Add(effect.effectName);
                }
            }
            
            return parts.Count > 0 ? string.Join(", ", parts) : "No special effects";
        }
    }
}
