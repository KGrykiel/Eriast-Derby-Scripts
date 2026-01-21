using Assets.Scripts.Events.EventCard;
using Assets.Scripts.Events.EventCard.EventCardTypes;
using Assets.Scripts.UI.Components;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Assets.Scripts.Tests
{
    internal class testEventPopup : MonoBehaviour
    {
        void Update()
        {
            // Press T to show test event card popup
            if (Keyboard.current != null && Keyboard.current[Key.T].wasPressedThisFrame)
            {
                // Check if EventCardUI exists in scene
                if (EventCardUI.Instance == null)
                {
                    Debug.LogError("[testEventPopup] EventCardUI not found in scene! " +
                        "Create the EventCardPopup prefab and add it to your Canvas.");
                    return;
                }
                
                var testCard = ScriptableObject.CreateInstance<ChoiceCard>();
                testCard.cardName = "Test Event";
                testCard.narrativeText = "A massive rockslide blocks your path! What do you do?";

                var testChoice = new CardChoice();
                testChoice.choiceText = "Attempt to Dodge";
                testCard.choices.Add(testChoice);

                // Phase 1: Show choices (suspense!)
                EventCardUI.Instance.ShowChoices(testCard, testCard.choices,
                    (choice) => 
                    {
                        Debug.Log($"Choice selected: {choice.choiceText}");
                        
                        // Phase 2: Show result after choice is made
                        var result = new CardResolutionResult(
                            success: false,
                            narrative: "Your driver swerves hard, but the vehicle can't dodge in time!\n\n" +
                                            "[Mobility Save: 12 vs DC 15] ❌\n\n" +
                                            "Your chassis takes 14 bludgeoning damage!"
                        );
                        
                        EventCardUI.Instance.ShowResult(result, 
                            onAcknowledge: () => Debug.Log("Result acknowledged, popup closed"));
                    });
            }
        }
    }
}
