using System;
using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts.Effects;
using Assets.Scripts.Combat.SkillChecks;
using Assets.Scripts.Combat.Saves;

namespace Assets.Scripts.Events.EventCard.EventCardTypes
{
    /// <summary>
    /// Choice card that presents 2-4 options to the player.
    /// Creates player agency and encourages team discussion.
    /// ~20% of stage decks.
    /// 
    /// Each choice can require different types of checks:
    /// - Skill Check: Active attempt (navigating, repairing, etc.)
    /// - Saving Throw: Reactive resistance (dodging, enduring, etc.)
    /// - No Check: Guaranteed outcome
    /// 
    /// Examples:
    /// - "Burning Barricade"
    ///   A: Ram through (Mobility save or take fire damage)
    ///   B: Find detour (Perception check or waste time)
    ///   C: Extinguish (No check, costs time but safe)
    /// </summary>
    [CreateAssetMenu(fileName = "New Choice Card", menuName = "Racing/Event Cards/Choice Card")]
    public class ChoiceCard : EventCard
    {
        [Header("Choices")]
        [Tooltip("Available choices (2-4 options)")]
        public List<CardChoice> choices = new List<CardChoice>();
        
        public override CardResolutionResult Resolve(Vehicle vehicle, Stage stage)
        {
            // For players: Would present UI and wait for choice
            // For now, just auto-pick first choice until UI is implemented
            Debug.LogWarning($"[ChoiceCard] Player choice UI not implemented yet. Auto-selecting first choice for {cardName}");
            
            if (choices.Count == 0)
            {
                Debug.LogError($"[ChoiceCard] {cardName} has no choices defined!");
                return new CardResolutionResult(false, "No choices available");
            }
            
            return ResolveChoice(choices[0], vehicle, stage);
        }
        
        public override CardResolutionResult AutoResolve(Vehicle vehicle, Stage stage)
        {
            // For NPCs: Pick best choice based on situation
            // For now, just pick first valid choice until AI is implemented
            if (choices.Count == 0)
            {
                Debug.LogError($"[ChoiceCard] {cardName} has no choices defined!");
                return new CardResolutionResult(false, "No choices available");
            }
            
            return ResolveChoice(choices[0], vehicle, stage);
        }
        
        /// <summary>
        /// Executes a specific choice.
        /// </summary>
        private CardResolutionResult ResolveChoice(CardChoice choice, Vehicle vehicle, Stage stage)
        {
            // Route based on check type
            switch (choice.checkType)
            {
                case ChoiceCheckType.None:
                    // No check - guaranteed outcome
                    ApplyEffects(choice.effects, vehicle);
                    return new CardResolutionResult(true, choice.outcomeNarrative);
                
                case ChoiceCheckType.SkillCheck:
                    return ResolveSkillCheck(choice, vehicle);
                
                case ChoiceCheckType.SavingThrow:
                    return ResolveSavingThrow(choice, vehicle);
                
                default:
                    Debug.LogError($"[ChoiceCard] Unknown check type: {choice.checkType}");
                    return new CardResolutionResult(false, "Invalid choice configuration");
            }
        }
        
        /// <summary>
        /// Resolves a choice that requires a skill check (active attempt).
        /// </summary>
        private CardResolutionResult ResolveSkillCheck(CardChoice choice, Vehicle vehicle)
        {
            if (!choice.skillCheckType.HasValue)
            {
                Debug.LogError($"[ChoiceCard] SkillCheck choice '{choice.choiceText}' has no skill type defined!");
                return new CardResolutionResult(false, "Invalid skill check configuration");
            }
            
            // Chassis makes all checks for now (universal, always present)
            if (vehicle.chassis == null)
            {
                Debug.LogError($"[ChoiceCard] Vehicle {vehicle.vehicleName} has no chassis!");
                return new CardResolutionResult(false, "No chassis to make check!");
            }
            
            var checkResult = SkillCheckCalculator.PerformSkillCheck(
                vehicle.chassis, 
                choice.skillCheckType.Value, 
                choice.dc);
            
            if (checkResult.Succeeded == true)
            {
                ApplyEffects(choice.effects, vehicle);
                return new CardResolutionResult(true, choice.outcomeNarrative);
            }
            else
            {
                ApplyEffects(choice.failureEffects, vehicle);
                return new CardResolutionResult(false, choice.failureNarrative);
            }
        }
        
        /// <summary>
        /// Resolves a choice that requires a saving throw (reactive resistance).
        /// </summary>
        private CardResolutionResult ResolveSavingThrow(CardChoice choice, Vehicle vehicle)
        {
            if (!choice.saveType.HasValue)
            {
                Debug.LogError($"[ChoiceCard] SavingThrow choice '{choice.choiceText}' has no save type defined!");
                return new CardResolutionResult(false, "Invalid saving throw configuration");
            }
            
            // Chassis makes all saves (represents vehicle body)
            if (vehicle.chassis == null)
            {
                Debug.LogError($"[ChoiceCard] Vehicle {vehicle.vehicleName} has no chassis!");
                return new CardResolutionResult(false, "No chassis to make save!");
            }
            
            var saveResult = SaveCalculator.PerformSavingThrow(
                vehicle.chassis, 
                choice.saveType.Value, 
                choice.dc);
            
            if (saveResult.Succeeded == true)
            {
                ApplyEffects(choice.effects, vehicle);
                return new CardResolutionResult(true, choice.outcomeNarrative);
            }
            else
            {
                ApplyEffects(choice.failureEffects, vehicle);
                return new CardResolutionResult(false, choice.failureNarrative);
            }
        }
    }
    
    /// <summary>
    /// Type of check a choice requires.
    /// </summary>
    public enum ChoiceCheckType
    {
        /// <summary>No check required - guaranteed outcome</summary>
        None,
        
        /// <summary>Active skill check (navigating, searching, repairing)</summary>
        SkillCheck,
        
        /// <summary>Passive saving throw (dodging, resisting, enduring)</summary>
        SavingThrow
    }
    
    /// <summary>
    /// Represents a single choice option in a ChoiceCard.
    /// </summary>
    [Serializable]
    public class CardChoice
    {
        [Tooltip("Text shown to player (e.g., 'Ram through the flames')")]
        public string choiceText = "Choice";
        
        [Header("Check Configuration")]
        [Tooltip("Type of check this choice requires")]
        public ChoiceCheckType checkType = ChoiceCheckType.None;
        
        [Tooltip("Skill check type (if checkType = SkillCheck)")]
        public SkillCheckType? skillCheckType = null;
        
        [Tooltip("Save type (if checkType = SavingThrow)")]
        public SaveType? saveType = null;
        
        [Tooltip("Difficulty class for the check/save")]
        public int dc = 15;
        
        [Header("Effects")]
        [Tooltip("Effects applied if choice succeeds (or no check required)")]
        public List<EffectInvocation> effects = new List<EffectInvocation>();
        
        [Tooltip("Effects applied if check/save fails")]
        public List<EffectInvocation> failureEffects = new List<EffectInvocation>();
        
        [Header("Narrative")]
        [Tooltip("Narrative for successful outcome")]
        public string outcomeNarrative = "Choice made";
        
        [Tooltip("Narrative for failed check/save")]
        public string failureNarrative = "Choice failed";
    }
}
