using System;
using System.Collections.Generic;
using UnityEngine;
using SerializeReferenceEditor;
using Assets.Scripts.Combat.Rolls.RollSpecs;
using Assets.Scripts.Entities.Vehicles;

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
            var ctx = new RollContext { Target = vehicle };
            bool success = RollNodeExecutor.Execute(choice.rollNode, ctx, this.name);

            string narrative = success ? choice.rollNode?.successNarrative : choice.rollNode?.failureNarrative;
            if (string.IsNullOrEmpty(narrative))
                narrative = choice.choiceText;

            return new CardResolutionResult(success, narrative);
        }
    }

    [Serializable]
    public class CardChoice
    {
        [Tooltip("Text shown to player (e.g., 'Ram through the flames')")]
        public string choiceText = "Choice";

        [SerializeReference, SR]
        [Tooltip("The full resolution of this choice: roll type, DC, success and failure effects, optional chain.")]
        public RollNode rollNode;
    }
}
