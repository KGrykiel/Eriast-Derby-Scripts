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
            
            var button = buttonObj.GetComponent<Button>();
            if (button != null)
            {
                button.onClick.AddListener(() => 
                {
                    RollTooltip.Hide();
                    onClick?.Invoke();
                });
            }
            
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
            if (popupPanel != null)
            {
                popupPanel.SetActive(true);
            }
        }
        
        private void Hide()
        {
            if (popupPanel != null)
                popupPanel.SetActive(false);
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
        
        private string GetEffectDescription(EffectInvocation invocation)
        {
            if (invocation?.effect == null) return "Unknown effect";
            
            string effectType = invocation.effect.GetType().Name;
            
            string description = effectType.Replace("Effect", "");

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
