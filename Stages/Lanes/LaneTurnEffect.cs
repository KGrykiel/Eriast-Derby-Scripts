using System;
using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts.Combat.SkillChecks;
using Assets.Scripts.Combat.Saves;

namespace Assets.Scripts.Stages.Lanes
{
    /// <summary>
    /// Defines an effect that triggers every turn for vehicles in a lane.
    /// 
    /// Examples:
    /// - "Cliff Edge": Mobility check DC 16 or take 2d10 falling damage
    /// - "Healing Spring": No check, restore 1d6 HP each turn
    /// - "Burning Road": Reflex save DC 14, success = half fire damage
    /// - "Rough Terrain": No check, always applies -2 speed modifier
    /// 
    /// Design: Similar to CardChoice from ChoiceCard, but automatic (no player choice).
    /// Can have optional check, and effects for success/failure/always.
    /// </summary>
    [Serializable]
    public class LaneTurnEffect
    {
        [Header("Identity")]
        [Tooltip("Display name for this effect (e.g., 'Cliff Edge Hazard')")]
        public string effectName = "Lane Effect";
        
        [TextArea(2, 4)]
        [Tooltip("Narrative description shown when effect triggers")]
        public string description = "";
        
        [Header("Check Configuration (Optional)")]
        [Tooltip("Type of check required (None = effects always apply)")]
        public LaneCheckType checkType = LaneCheckType.None;
        
        [Tooltip("Skill check spec (if checkType = SkillCheck)")]
        public SkillCheckSpec checkSpec;
        
        [Tooltip("Save spec (if checkType = SavingThrow)")]
        public SaveSpec saveSpec;
        
        [Tooltip("Difficulty class for the check/save")]
        [Range(5, 30)]
        public int dc = 15;
        
        [Header("Effects")]
        [Tooltip("Effects applied on successful check (or always if no check)")]
        public List<EffectInvocation> onSuccess = new();
        
        [Tooltip("Effects applied on failed check (ignored if no check)")]
        public List<EffectInvocation> onFailure = new();
        
        [Header("Narrative")]
        [Tooltip("Text shown on success (or when no check required)")]
        public string successNarrative = "";
        
        [Tooltip("Text shown on failure")]
        public string failureNarrative = "";
    }
    
    /// <summary>
    /// Type of check a lane turn effect can require.
    /// </summary>
    public enum LaneCheckType
    {
        /// <summary>No check - effects always apply (e.g., healing spring, rough terrain)</summary>
        None,
        
        /// <summary>Active skill check (e.g., Mobility to navigate cliff edge)</summary>
        SkillCheck,
        
        /// <summary>Passive saving throw (e.g., Reflex to dodge falling rocks)</summary>
        SavingThrow
    }
}
