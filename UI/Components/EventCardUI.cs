using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using Assets.Scripts.Events.EventCard.EventCardTypes;
using Assets.Scripts.Events.EventCard;
using Assets.Scripts.Combat.Restoration;
using Assets.Scripts.Combat.Rolls.RollSpecs;
using Assets.Scripts.Combat.Rolls.RollSpecs.SpecTypes;
using Assets.Scripts.Combat.Damage.FormulaProviders.SpecificProviders;
using Assets.Scripts.Effects.Invocations;
using Assets.Scripts.Effects.EffectTypes.EntityEffects;
using Assets.Scripts.UI.Tabs.EventFeed;

namespace Assets.Scripts.UI.Components
{
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
        
        private Action<CardChoice> currentCallback;
        private List<GameObject> spawnedButtons = new();
        
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            Hide();
        }
        public void ShowChoices(
            EventCard card, 
            List<CardChoice> choices, 
            Action<CardChoice> onChoiceSelected)
        {
            if (card == null || choices == null || choices.Count == 0)
            {
                Debug.LogError("[EventCardUI] Cannot show event card with no choices!");
                return;
            }
            
            currentCallback = onChoiceSelected;
            
            if (titleText != null)
                titleText.text = card.cardName.ToUpper();
            
            if (narrativeText != null)
                narrativeText.text = card.narrativeText;
            
            ClearChoiceButtons();
            
            if (choiceButtonContainer != null && choiceButtonPrefab != null)
            {
                foreach (var choice in choices)
                {
                    CreateChoiceButton(choice, () => OnChoiceClicked(choice));
                }
            }
            
            Show();
        }
        
        public void ShowResult(CardResolutionResult result, Action onAcknowledge = null)
        {
            if (result == null)
            {
                Debug.LogError("[EventCardUI] Cannot show null result!");
                return;
            }
            
            ClearChoiceButtons();
            
            if (narrativeText != null)
            {
                narrativeText.text = result.narrativeOutcome;
            }
            
            if (choiceButtonContainer != null && choiceButtonPrefab != null)
            {
                CreateChoiceButton(
                    new CardChoice { choiceText = "Acknowledge" },
                    () =>
                    {
                        onAcknowledge?.Invoke();
                        Hide();
                    },
                    showTooltip: false);
            }
            
            Show();
        }
        private void CreateChoiceButton(CardChoice choice, Action onClick, bool showTooltip = true)
        {
            GameObject buttonObj = Instantiate(choiceButtonPrefab, choiceButtonContainer);
            spawnedButtons.Add(buttonObj);
            
            var buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.text = choice.choiceText;
            }
            
            if (buttonObj.TryGetComponent<Button>(out var button))
            {
                button.onClick.AddListener(() => 
                {
                    RollTooltip.Hide();
                    onClick?.Invoke();
                });
            }
            
            if (showTooltip)
            {
                if (!buttonObj.TryGetComponent<ChoiceButtonHover>(out var hoverComponent))
                {
                    hoverComponent = buttonObj.AddComponent<ChoiceButtonHover>();
                }
                hoverComponent.SetChoice(choice);
            }
        }
        
        private void OnChoiceClicked(CardChoice choice)
        {
            ClearChoiceButtons();
            
            currentCallback?.Invoke(choice);
            currentCallback = null;
        }
        
        private void ClearChoiceButtons()
        {
            foreach (var button in spawnedButtons)
            {
                if (button != null)
                    Destroy(button);
            }
            spawnedButtons.Clear();
        }
        
        private void Show()
        {
            IsShowing = true;
            if (popupPanel != null)
            {
                popupPanel.SetActive(true);
            }
        }

        private void Hide()
        {
            IsShowing = false;
            if (popupPanel != null)
                popupPanel.SetActive(false);
        }

        public bool IsShowing { get; private set; }
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
            if (choice.rollNode == null)
                return "<b>No check required</b>";

            return BuildNodeTooltip(choice.rollNode, indent: "", depth: 0);
        }

        private string BuildNodeTooltip(RollNode node, string indent, int depth)
        {
            if (node == null)
                return "";

            if (depth >= 3)
                return "<i>(\u2026)</i>";

            string tooltip = "";

            if (node.rollSpec is SaveSpec saveSpec)
                tooltip += $"<b>{saveSpec.DisplayName} Save DC {saveSpec.dc}</b>";
            else if (node.rollSpec is SkillCheckSpec checkSpec)
                tooltip += $"<b>{checkSpec.DisplayName} Check DC {checkSpec.dc}</b>";
            else
                tooltip += "<b>Always applies</b>";

            bool hasSuccessEffects = node.successEffects != null && node.successEffects.Count > 0;
            if (hasSuccessEffects || (node.onSuccessChains != null && node.onSuccessChains.Count > 0))
            {
                tooltip += $"\n{indent}<color=green><b>On Success:</b></color>";
                if (hasSuccessEffects)
                {
                    foreach (var effect in node.successEffects)
                    {
                        string desc = GetEffectDescription(effect);
                        if (desc != null)
                            tooltip += $"\n{indent}  \u2022 {desc}";
                    }
                }
                if (node.onSuccessChains != null)
                    foreach (var chain in node.onSuccessChains)
                        tooltip += $"\n{indent}  \u2192 {BuildNodeTooltip(chain, indent + "    ", depth + 1)}";
            }

            bool hasFailureEffects = node.failureEffects != null && node.failureEffects.Count > 0;
            if (hasFailureEffects || (node.onFailureChains != null && node.onFailureChains.Count > 0))
            {
                tooltip += $"\n{indent}<color=red><b>On Failure:</b></color>";
                if (hasFailureEffects)
                {
                    foreach (var effect in node.failureEffects)
                    {
                        string desc = GetEffectDescription(effect);
                        if (desc != null)
                            tooltip += $"\n{indent}  \u2022 {desc}";
                    }
                }
                if (node.onFailureChains != null)
                    foreach (var chain in node.onFailureChains)
                        tooltip += $"\n{indent}  \u2192 {BuildNodeTooltip(chain, indent + "    ", depth + 1)}";
            }

            return tooltip;
        }

        private string GetEffectDescription(IEffectInvocation invocation)
        {
            if (invocation == null) return null;

            object effect = invocation switch
            {
                EntityEffectInvocation e => e.effect,
                SeatEffectInvocation s   => s.effect,
                VehicleEffectInvocation v => v.effect,
                _ => null
            };

            if (effect == null) return null;

            string effectType = effect.GetType().Name;

            string description = effectType.Replace("Effect", "");

            if (effect is DamageEffect damageEffect && damageEffect.formulaProvider is StaticFormulaProvider staticProvider)
            {
                var formula = staticProvider.formula;
                description = $"Damage ({formula.baseDice}d{formula.dieSize}+{formula.bonus})";
            }
            else if (effect is DamageEffect weaponDamage && weaponDamage.formulaProvider is WeaponFormulaProvider)
            {
                description = "Damage (Weapon)";
            }
            else if (effect is ApplyEntityConditionEffect statusEffect && statusEffect.condition != null)
            {
                description = $"Apply: {statusEffect.condition.effectName}";
            }
            else if (effect is ResourceRestorationEffect restorationEffect)
            {
                string verb = restorationEffect.formula.isDrain ? "Drain" : "Restore";
                int expectedAmount = Mathf.Abs(RestorationCalculator.Compute(restorationEffect.formula));
                description = $"{verb} {expectedAmount} {restorationEffect.formula.resourceType}";
            }

            return description;
        }
    }
}
