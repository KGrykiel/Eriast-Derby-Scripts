using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using Assets.Scripts.StatusEffects;
using Assets.Scripts.Combat;

namespace Assets.Scripts.UI.Components
{
    /// <summary>
    /// UI component for displaying a single status effect icon with turn counter.
    /// Shows icon sprite + duration text, provides hover tooltip.
    /// 
    /// Usage: Attach to a GameObject with Image component (for icon) and child Text component (for duration).
    /// </summary>
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
        public Color buffColor = new Color(0.2f, 0.8f, 0.2f, 0.8f);
        
        [Tooltip("Background color for debuff effects (only used if tintIconByEffectType is true)")]
        public Color debuffColor = new Color(0.8f, 0.2f, 0.2f, 0.8f);
        
        [Tooltip("Background color for neutral effects (only used if tintIconByEffectType is true)")]
        public Color neutralColor = new Color(0.5f, 0.5f, 0.5f, 0.8f);
        
        // Private state
        private AppliedStatusEffect appliedEffect;
        private RectTransform rectTransform;
        
        void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            
            // Auto-find image if not set
            if (iconImage == null)
            {
                iconImage = GetComponent<Image>();
            }
            
            // Auto-find duration text if not set (check children)
            if (durationText == null)
            {
                durationText = GetComponentInChildren<TextMeshProUGUI>();
            }
        }
        
        /// <summary>
        /// Initialize this icon with a status effect.
        /// </summary>
        public void Initialize(AppliedStatusEffect effect)
        {
            appliedEffect = effect;
            UpdateDisplay();
        }
        
        /// <summary>
        /// Update the visual display (icon, duration, colors).
        /// Call this each turn to update duration display.
        /// </summary>
        public void UpdateDisplay()
        {
            if (appliedEffect == null || appliedEffect.template == null)
            {
                gameObject.SetActive(false);
                return;
            }
            
            gameObject.SetActive(true);
            
            // Set icon sprite
            if (iconImage != null)
            {
                // Use default icon if template has no icon
                Sprite spriteToUse = appliedEffect.template.icon != null ? appliedEffect.template.icon : defaultIcon;
                
                if (spriteToUse != null)
                {
                    iconImage.sprite = spriteToUse;
                    iconImage.enabled = true;
                }
                else
                {
                    // If no icon at all, show a colored square
                    iconImage.sprite = null;
                    iconImage.enabled = true;
                }
                
                // Only tint if enabled - otherwise keep sprite's original colors
                if (tintIconByEffectType)
                {
                    iconImage.color = GetEffectColor();
                }
                else
                {
                    iconImage.color = Color.white; // No tint
                }
            }
            
            // Set duration text
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
        
        /// <summary>
        /// Determine the background color based on whether this is a buff or debuff.
        /// </summary>
        private Color GetEffectColor()
        {
            if (appliedEffect?.template == null)
                return neutralColor;
            
            // Check if this is a buff or debuff
            bool isBuff = DetermineIfBuff(appliedEffect.template);
            
            return isBuff ? buffColor : debuffColor;
        }
        
        /// <summary>
        /// Determine if a status effect is a buff or debuff.
        /// Positive modifiers, healing, energy restore = buff.
        /// Negative modifiers, damage, restrictions = debuff.
        /// </summary>
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
        
        // ==================== HOVER TOOLTIP ====================
        
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (appliedEffect == null || appliedEffect.template == null) return;
            
            // Get tooltip content from CombatLogManager (single source of truth)
            string tooltipContent = CombatLogManager.FormatStatusEffectTooltip(appliedEffect);
            
            // Show tooltip positioned near this icon
            RollTooltip.ShowNow(tooltipContent, rectTransform);
        }
        
        public void OnPointerExit(PointerEventData eventData)
        {
            RollTooltip.Hide();
        }
        
        /// <summary>
        /// Clean up (called when icon is being destroyed or pooled).
        /// </summary>
        void OnDisable()
        {
            // Hide tooltip if this icon is being disabled
            if (RollTooltip.Instance != null)
            {
                RollTooltip.Hide();
            }
        }
    }
}
