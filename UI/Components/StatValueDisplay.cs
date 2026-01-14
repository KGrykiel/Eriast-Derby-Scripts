using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using Assets.Scripts.Combat;
using Assets.Scripts.Core;

namespace Assets.Scripts.UI.Components
{
    /// <summary>
    /// UI component for displaying a stat value with color coding and tooltip.
    /// Green = buffed, Red = debuffed, White = base value.
    /// Hover shows modifier breakdown tooltip (uses StatCalculator for data).
    /// 
    /// Usage: Attach to a TextMeshProUGUI component.
    /// </summary>
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class StatValueDisplay : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("Colors")]
        [Tooltip("Color when stat is buffed (positive modifiers)")]
        public Color buffedColor = new Color(0.27f, 1f, 0.27f); // #44FF44
        
        [Tooltip("Color when stat is debuffed (negative modifiers)")]
        public Color debuffedColor = new Color(1f, 0.27f, 0.27f); // #FF4444
        
        [Tooltip("Color when stat is at base value (no modifiers)")]
        public Color normalColor = Color.white;
        
        // Private state
        private TextMeshProUGUI textComponent;
        private RectTransform rectTransform;
        
        // Tooltip data
        private Entity tooltipEntity;
        private Attribute tooltipAttribute;
        private float tooltipBaseValue;
        private float tooltipFinalValue;
        
        void Awake()
        {
            textComponent = GetComponent<TextMeshProUGUI>();
            rectTransform = GetComponent<RectTransform>();
        }
        
        /// <summary>
        /// Update the displayed stat value with color coding.
        /// Uses StatCalculator to determine if stat is modified.
        /// </summary>
        /// <param name="entity">Entity for tooltip breakdown (can be null if no tooltip)</param>
        /// <param name="attribute">Attribute for tooltip breakdown</param>
        /// <param name="baseValue">Base value (unmodified)</param>
        /// <param name="finalValue">Final value (with modifiers)</param>
        /// <param name="displayText">Custom display text (if null, uses finalValue)</param>
        public void UpdateDisplay(
            Entity entity,
            Attribute attribute,
            float baseValue,
            float finalValue,
            string displayText = null)
        {
            if (textComponent == null) return;
            
            // Store tooltip data
            tooltipEntity = entity;
            tooltipAttribute = attribute;
            tooltipBaseValue = baseValue;
            tooltipFinalValue = finalValue;
            
            // Set display text
            textComponent.text = displayText ?? finalValue.ToString("F1");
            
            // Set color based on modifiers
            float totalModifiers = finalValue - baseValue;
            
            if (Mathf.Approximately(totalModifiers, 0f))
            {
                textComponent.color = normalColor;
            }
            else if (totalModifiers > 0)
            {
                textComponent.color = buffedColor;
            }
            else
            {
                textComponent.color = debuffedColor;
            }
        }
        
        /// <summary>
        /// Update display with just final value (no tooltip, always white).
        /// </summary>
        public void UpdateDisplay(float value, string displayText = null)
        {
            if (textComponent == null) return;
            
            textComponent.text = displayText ?? value.ToString("F1");
            textComponent.color = normalColor;
            
            // Clear tooltip data
            ClearTooltipData();
        }
        
        /// <summary>
        /// Clear tooltip data so hovering won't show outdated info.
        /// </summary>
        public void ClearTooltipData()
        {
            tooltipEntity = null;
        }
        
        /// <summary>
        /// Update display with color but no tooltip.
        /// </summary>
        public void UpdateDisplaySimple(float baseValue, float finalValue, string displayText = null)
        {
            if (textComponent == null) return;
            
            textComponent.text = displayText ?? finalValue.ToString("F1");
            
            float totalModifiers = finalValue - baseValue;
            
            if (Mathf.Approximately(totalModifiers, 0f))
            {
                textComponent.color = normalColor;
            }
            else if (totalModifiers > 0)
            {
                textComponent.color = buffedColor;
            }
            else
            {
                textComponent.color = debuffedColor;
            }
            
            // Clear tooltip data
            tooltipEntity = null;
        }
        
        // ==================== HOVER TOOLTIP ====================
        
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (tooltipEntity == null) return;
            
            // Get tooltip content from CombatLogManager (single source of truth for formatting)
            // CombatLogManager uses StatCalculator internally for data gathering
            string tooltipContent = CombatLogManager.FormatStatBreakdown(
                tooltipEntity,
                tooltipAttribute,
                tooltipBaseValue,
                tooltipFinalValue);
            
            // Show tooltip positioned near this text
            RollTooltip.ShowNow(tooltipContent, rectTransform);
        }
        
        public void OnPointerExit(PointerEventData eventData)
        {
            RollTooltip.Hide();
        }
        
        void OnDisable()
        {
            // Hide tooltip if this stat display is being disabled
            if (RollTooltip.Instance != null)
            {
                RollTooltip.Hide();
            }
        }
    }
}
