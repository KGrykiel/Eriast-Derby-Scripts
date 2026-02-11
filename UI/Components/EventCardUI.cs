using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using Assets.Scripts.Events.EventCard.EventCardTypes;
using Assets.Scripts.Events.EventCard;
using Assets.Scripts.Combat.Damage;

namespace Assets.Scripts.UI.Components
{
    /// <summary>
    /// EU4-style modal popup for presenting event cards to the player.
    /// Singleton pattern - only one event card can be shown at a time.
    /// 
    /// Usage:
    /// 1. Present choices: EventCardUI.Instance.ShowChoices(card, choices, callback)
    /// 2. Wait for player click
    /// 3. Callback is invoked with selected choice
    /// 4. Show results: EventCardUI.Instance.ShowResult(result)
    /// </summary>
    public class EventCardUI : MonoBehaviour
    {
        public static EventCardUI Instance { get; private set; }
        
        [Header("UI References")]
        [Tooltip("Root panel that contains all UI elements")]
        public GameObject popupPanel;
        
        [Tooltip("Title text (e.g., 'ROCKSLIDE!')")]
        public TextMeshProUGUI titleText;
        
        [Tooltip("Main narrative text (reused for both choices and results)")]
        public TextMeshProUGUI narrativeText;
        
        [Tooltip("Container for choice buttons")]
        public Transform choiceButtonContainer;
        
        [Tooltip("Prefab for individual choice buttons")]
        public GameObject choiceButtonPrefab;
        
        // Runtime state
        private Action<CardChoice> currentCallback;
        private List<GameObject> spawnedButtons = new();
        
        private void Awake()
        {
            // Singleton setup
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            
            // Start hidden
            Hide();
        }
        
        /// <summary>
        /// Shows the event card with choices for player selection.
        /// Blocks until player clicks a choice button.
        /// </summary>
        public void ShowChoices(
            Events.EventCard.EventCard card, 
            List<CardChoice> choices, 
            Action<CardChoice> onChoiceSelected)
        {
            if (card == null || choices == null || choices.Count == 0)
            {
                Debug.LogError("[EventCardUI] Cannot show event card with no choices!");
                return;
            }
            
            currentCallback = onChoiceSelected;
            
            // Set title and narrative
            if (titleText != null)
                titleText.text = card.cardName.ToUpper();
            
            if (narrativeText != null)
                narrativeText.text = card.narrativeText;
            
            // Clear old choice buttons
            ClearChoiceButtons();
            
            // Create choice buttons
            if (choiceButtonContainer != null && choiceButtonPrefab != null)
            {
                foreach (var choice in choices)
                {
                    CreateChoiceButton(choice, () => OnChoiceClicked(choice));
                }
            }
            
            // Show popup
            Show();
        }
        
        /// <summary>
        /// Shows the resolution result (success/failure narrative).
        /// Shows single "Acknowledge" button to dismiss.
        /// </summary>
        public void ShowResult(CardResolutionResult result, Action onAcknowledge = null)
        {
            if (result == null)
            {
                Debug.LogError("[EventCardUI] Cannot show null result!");
                return;
            }
            
            // Clear choice buttons
            ClearChoiceButtons();
            
            // Update narrative text to show result
            if (narrativeText != null)
            {
                narrativeText.text = result.narrativeOutcome;
            }
            
            // Create single "Acknowledge" button (no tooltip)
            if (choiceButtonContainer != null && choiceButtonPrefab != null)
            {
                CreateChoiceButton(
                    new CardChoice { choiceText = "Acknowledge" },
                    () =>
                    {
                        onAcknowledge?.Invoke();
                        Hide();
                    },
                    showTooltip: false); // Don't show tooltip on acknowledge button
            }
            
            Show();
        }
        
        /// <summary>
        /// Creates a button for a single choice option.
        /// </summary>
        private void CreateChoiceButton(CardChoice choice, Action onClick, bool showTooltip = true)
        {
            GameObject buttonObj = Instantiate(choiceButtonPrefab, choiceButtonContainer);
            spawnedButtons.Add(buttonObj);
            
            // Setup button text
            var buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.text = choice.choiceText;
            }
            
            // Setup button callback
            var button = buttonObj.GetComponent<Button>();
            if (button != null)
            {
                button.onClick.AddListener(() => 
                {
                    // Hide tooltip immediately when button is clicked
                    RollTooltip.Hide();
                    onClick?.Invoke();
                });
            }
            
            // Setup tooltip hover component (only if requested)
            if (showTooltip)
            {
                var hoverComponent = buttonObj.GetComponent<ChoiceButtonHover>();
                if (hoverComponent == null)
                {
                    hoverComponent = buttonObj.AddComponent<ChoiceButtonHover>();
                }
                hoverComponent.SetChoice(choice);
            }
        }
        
        /// <summary>
        /// Called when player clicks a choice button.
        /// </summary>
        private void OnChoiceClicked(CardChoice choice)
        {
            // Clear buttons immediately (no going back!)
            ClearChoiceButtons();
            
            // Invoke callback
            currentCallback?.Invoke(choice);
            currentCallback = null;
        }
        
        /// <summary>
        /// Clears all spawned choice buttons.
        /// </summary>
        private void ClearChoiceButtons()
        {
            foreach (var button in spawnedButtons)
            {
                if (button != null)
                    Destroy(button);
            }
            spawnedButtons.Clear();
        }
        
        /// <summary>
        /// Shows the popup panel.
        /// </summary>
        private void Show()
        {
            if (popupPanel != null)
            {
                popupPanel.SetActive(true);
                // Don't call SetAsLastSibling - let RollTooltip handle its own Z-order
            }
        }
        
        /// <summary>
        /// Hides the popup panel.
        /// </summary>
        private void Hide()
        {
            if (popupPanel != null)
                popupPanel.SetActive(false);
        }
        
        /// <summary>
        /// Generates tooltip text for a choice (shows DC, effects preview).
        /// </summary>
        private string GetChoiceTooltip(CardChoice choice)
        {
            var tooltip = "";
            
            // Show check requirements
            if (choice.checkType == ChoiceCheckType.SavingThrow)
            {
                tooltip += $"{choice.saveSpec.DisplayName} Save DC {choice.dc}\n";
            }
            else if (choice.checkType == ChoiceCheckType.SkillCheck)
            {
                tooltip += $"{choice.checkSpec.DisplayName} Check DC {choice.dc}\n";
            }
            else
            {
                tooltip += "No check required\n";
            }
            
            // Show effects preview (simplified)
            if (choice.effects != null && choice.effects.Count > 0)
            {
                tooltip += $"\nOn Success: {choice.effects.Count} effect(s)";
            }
            
            if (choice.failureEffects != null && choice.failureEffects.Count > 0)
            {
                tooltip += $"\nOn Failure: {choice.failureEffects.Count} effect(s)";
            }
            
            return tooltip;
        }
    }
    
    /// <summary>
    /// Hover component for choice buttons. Shows tooltip on hover.
    /// </summary>
    public class ChoiceButtonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private CardChoice choice;
        private string cachedTooltipContent;
        private RectTransform rectTransform;
        
        void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
        }
        
        public void SetChoice(CardChoice choice)
        {
            this.choice = choice;
            cachedTooltipContent = BuildTooltip(choice);
        }
        
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!string.IsNullOrEmpty(cachedTooltipContent) && RollTooltip.Instance != null)
            {
                RollTooltip.ShowNow(cachedTooltipContent, rectTransform);
            }
        }
        
        public void OnPointerExit(PointerEventData eventData)
        {
            RollTooltip.Hide();
        }
        
        private string BuildTooltip(CardChoice choice)
        {
            var tooltip = "";
            
            // Show check requirements
            if (choice.checkType == ChoiceCheckType.SavingThrow)
            {
                tooltip += $"<b>{choice.saveSpec.DisplayName} Save DC {choice.dc}</b>\n";
            }
            else if (choice.checkType == ChoiceCheckType.SkillCheck)
            {
                tooltip += $"<b>{choice.checkSpec.DisplayName} Check DC {choice.dc}</b>\n";
            }
            else
            {
                tooltip += "<b>No check required</b>\n";
            }
            
            // Show success effects details
            if (choice.effects != null && choice.effects.Count > 0)
            {
                tooltip += "\n<color=green><b>On Success:</b></color>";
                foreach (var effect in choice.effects)
                {
                    if (effect != null && effect.effect != null)
                    {
                        tooltip += $"\n  • {GetEffectDescription(effect)}";
                    }
                }
            }
            
            // Show failure effects details
            if (choice.failureEffects != null && choice.failureEffects.Count > 0)
            {
                tooltip += "\n<color=red><b>On Failure:</b></color>";
                foreach (var effect in choice.failureEffects)
                {
                    if (effect != null && effect.effect != null)
                    {
                        tooltip += $"\n  • {GetEffectDescription(effect)}";
                    }
                }
            }
            
            return tooltip;
        }
        
        /// <summary>
        /// Gets a human-readable description of an effect.
        /// </summary>
        private string GetEffectDescription(EffectInvocation invocation)
        {
            if (invocation?.effect == null) return "Unknown effect";
            
            // Get effect type name (e.g., "DamageEffect")
            string effectType = invocation.effect.GetType().Name;
            
            // Try to get a description from the effect if it has one
            // Most effects don't have description properties, so we'll use type name
            string description = effectType.Replace("Effect", "");

            // For damage effects, show formula if it's static (can't introspect weapon at design time)
            if (invocation.effect is DamageEffect damageEffect && damageEffect.formulaProvider is StaticFormulaProvider staticProvider)
            {
                var formula = staticProvider.formula;
                description = $"Damage ({formula.baseDice}d{formula.dieSize}+{formula.bonus})";
            }
            else if (invocation.effect is DamageEffect weaponDamage && weaponDamage.formulaProvider is WeaponFormulaProvider)
            {
                description = "Damage (Weapon)";
            }
            else if (invocation.effect is ApplyStatusEffect statusEffect && statusEffect.statusEffect != null)
            {
                description = $"Apply: {statusEffect.statusEffect.effectName}";
            }
            else if (invocation.effect is ResourceRestorationEffect restorationEffect)
            {
                description = $"{(restorationEffect.amount > 0 ? "Restore" : "Drain")} {Mathf.Abs(restorationEffect.amount)} {restorationEffect.resourceType}";
            }
            
            return description;
        }
    }
}
