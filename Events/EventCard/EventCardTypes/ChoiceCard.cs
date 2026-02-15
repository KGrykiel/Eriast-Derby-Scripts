using System;
using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts.Combat.SkillChecks;
using Assets.Scripts.Combat.Saves;

namespace Assets.Scripts.Events.EventCard.EventCardTypes
{
    /// <summary>
    /// Presents several options to the player. Each option can require a skill check, save, or no check.
    /// Also used for non-choice events since they're equivalent to a choice card with 1 option.
    /// Kind of like events in EU4.
    /// </summary>
    [CreateAssetMenu(fileName = "New Choice Card", menuName = "Racing/Event Cards/Choice Card")]
    public class ChoiceCard : EventCard
    {
        [Header("Choices")]
        [Tooltip("Available choices (2-4 options)")]
        public List<CardChoice> choices = new();

        public override CardResolutionResult Resolve(Vehicle vehicle)
        {
            if (choices.Count == 0)
            {
                Debug.LogError($"[ChoiceCard] {cardName} has no choices defined!");
                return new CardResolutionResult(false, "No choices available");
            }

            if (UI.Components.EventCardUI.Instance == null)
            {
                Debug.LogWarning($"[ChoiceCard] EventCardUI not found! Auto-selecting first choice for {cardName}");
                return ResolveChoice(choices[0], vehicle);
            }

            UI.Components.EventCardUI.Instance.ShowChoices(this, choices, (selectedChoice) =>
            {
                var result = ResolveChoice(selectedChoice, vehicle);

                UI.Components.EventCardUI.Instance.ShowResult(result, () =>
                {
                    LogCardEvent(vehicle, result);
                });
            });

            return new CardResolutionResult(true, "Awaiting player choice...");
        }

        /// <summary>
        /// Entry for AI controlled vehicles, right now picks the first option but could be expanded to evaluate choices based on situation.
        /// </summary>
        public override CardResolutionResult AutoResolve(Vehicle vehicle)
        {
            if (choices.Count == 0)
            {
                Debug.LogError($"[ChoiceCard] {cardName} has no choices defined!");
                return new CardResolutionResult(false, "No choices available");
            }

            return ResolveChoice(choices[0], vehicle);
        }

        private CardResolutionResult ResolveChoice(CardChoice choice, Vehicle vehicle)
        {
            switch (choice.checkType)
            {
                case ChoiceCheckType.None:
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
        
        private CardResolutionResult ResolveSkillCheck(CardChoice choice, Vehicle vehicle)
        {
            var checkResult = SkillCheckPerformer.Execute(
                vehicle, choice.checkSpec, choice.dc, causalSource: this);

            if (checkResult.Roll.Success)
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
        
        private CardResolutionResult ResolveSavingThrow(CardChoice choice, Vehicle vehicle)
        {
            var saveResult = SavePerformer.Execute(
                vehicle, choice.saveSpec, choice.dc, causalSource: this);

            if (saveResult.Roll.Success)
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
    
    public enum ChoiceCheckType
    {
        /// <summary>No check required - guaranteed outcome</summary>
        None,
        
        /// <summary>Active skill check (navigating, searching, repairing)</summary>
        SkillCheck,
        
        /// <summary>Passive saving throw (dodging, resisting, enduring)</summary>
        SavingThrow
    }
    
    [Serializable]
    public class CardChoice
    {
        [Tooltip("Text shown to player (e.g., 'Ram through the flames')")]
        public string choiceText = "Choice";
        
        [Header("Check Configuration")]
        [Tooltip("Type of check this choice requires")]
        public ChoiceCheckType checkType = ChoiceCheckType.None;
        
        [Tooltip("Skill check spec (if checkType = SkillCheck)")]
        public SkillCheckSpec checkSpec;
        
        [Tooltip("Save spec (if checkType = SavingThrow)")]
        public SaveSpec saveSpec;
        
        [Tooltip("Difficulty class for the check/save")]
        public int dc = 15;
        
        [Header("Effects")]
        [Tooltip("Effects applied if choice succeeds (or no check required)")]
        public List<EffectInvocation> effects = new();
        
        [Tooltip("Effects applied if check/save fails")]
        public List<EffectInvocation> failureEffects = new();
        
        [Header("Narrative")]
        [Tooltip("Narrative for successful outcome")]
        public string outcomeNarrative = "Choice made";
        
        [Tooltip("Narrative for failed check/save")]
        public string failureNarrative = "Choice failed";
    }
}
