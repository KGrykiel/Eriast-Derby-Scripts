using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using Assets.Scripts.StatusEffects;
using Assets.Scripts.Combat.Logging;

namespace Assets.Scripts.UI.Components
{
    [RequireComponent(typeof(Image))]
    internal class StatusEffectIcon : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("UI References")]
        [Tooltip("Image component for the status effect icon")]
        public Image iconImage;
        
        [Tooltip("Text component for displaying duration (turns remaining)")]
        public TextMeshProUGUI durationText;
        
        [Header("Default Icon")]
        [Tooltip("Default icon sprite if status effect has no icon")]
        public Sprite defaultIcon;
        
        [Header("Color Settings")]
        [Tooltip("If true, tint the icon based on buff/debuff. If false, keep original sprite colors.")]
        public bool tintIconByEffectType = false;
        
        [Tooltip("Background color for buff effects (only used if tintIconByEffectType is true)")]
        public Color buffColor = new(0.2f, 0.8f, 0.2f, 0.8f);
        
        [Tooltip("Background color for debuff effects (only used if tintIconByEffectType is true)")]
        public Color debuffColor = new(0.8f, 0.2f, 0.2f, 0.8f);
        
        [Tooltip("Background color for neutral effects (only used if tintIconByEffectType is true)")]
        public Color neutralColor = new(0.5f, 0.5f, 0.5f, 0.8f);
        
        private AppliedStatusEffect appliedEffect;
        private RectTransform rectTransform;

        void Awake()
        {
            rectTransform = GetComponent<RectTransform>();

            if (iconImage == null)
                iconImage = GetComponent<Image>();

            if (durationText == null)
                durationText = GetComponentInChildren<TextMeshProUGUI>();
        }
        
        public void Initialize(AppliedStatusEffect effect)
        {
            appliedEffect = effect;
            UpdateDisplay();
        }

        public void UpdateDisplay()
        {
            if (appliedEffect == null || appliedEffect.template == null)
            {
                gameObject.SetActive(false);
                return;
            }
            
            gameObject.SetActive(true);
            
            if (iconImage != null)
            {
                Sprite spriteToUse = appliedEffect.template.icon != null ? appliedEffect.template.icon : defaultIcon;

                if (spriteToUse != null)
                {
                    iconImage.sprite = spriteToUse;
                    iconImage.enabled = true;
                }
                else
                {
                    iconImage.sprite = null;
                    iconImage.enabled = true;
                }

                if (tintIconByEffectType)
                    iconImage.color = GetEffectColor();
                else
                    iconImage.color = Color.white;
            }

            if (durationText != null)
            {
                if (appliedEffect.IsIndefinite)
                {
                    durationText.text = "∞";
                }
                else
                {
                    durationText.text = appliedEffect.turnsRemaining.ToString();
                }
            }
        }
        
        private Color GetEffectColor()
        {
            if (appliedEffect?.template == null)
                return neutralColor;

            bool isBuff = DetermineIfBuff(appliedEffect.template);
            return isBuff ? buffColor : debuffColor;
        }

        private bool DetermineIfBuff(StatusEffect statusEffect)
        {
            float totalModifierValue = 0f;
            foreach (var mod in statusEffect.modifiers)
            {
                totalModifierValue += mod.value;
            }
            
            bool hasPeriodicDamage = false;
            bool hasPeriodicHealing = false;
            
            foreach (var periodic in statusEffect.periodicEffects)
            {
                if (periodic.type == PeriodicEffectType.Damage)
                    hasPeriodicDamage = true;
                if (periodic.type == PeriodicEffectType.Healing)
                    hasPeriodicHealing = true;
            }
            
            bool hasBehavioralRestrictions = statusEffect.behavioralEffects != null &&
                (statusEffect.behavioralEffects.preventsActions ||
                 statusEffect.behavioralEffects.preventsMovement ||
                 statusEffect.behavioralEffects.damageAmplification > 1f);
            
            if (hasPeriodicDamage || hasBehavioralRestrictions)
                return false;
            
            if (hasPeriodicHealing || totalModifierValue > 0)
                return true;
            
            return totalModifierValue >= 0;
        }
        
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (appliedEffect == null || appliedEffect.template == null) return;

            string tooltipContent = CombatFormatter.FormatStatusEffectTooltip(appliedEffect);
            RollTooltip.ShowNow(tooltipContent, rectTransform);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            RollTooltip.Hide();
        }

        void OnDisable()
        {
            if (RollTooltip.Instance != null)
                RollTooltip.Hide();
        }
    }
}
