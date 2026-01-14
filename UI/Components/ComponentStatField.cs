using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Assets.Scripts.Core;

namespace Assets.Scripts.UI.Components
{
    /// <summary>
    /// Represents a single stat field for a component (HP, AC, Speed, Energy, etc.)
    /// Can include a label, value text, bar, and StatValueDisplay for modifiers.
    /// Dynamically shown/hidden based on whether the component has this stat.
    /// </summary>
    public class ComponentStatField : MonoBehaviour
    {
        [Header("Configuration")]
        [Tooltip("Which attribute does this field represent? (Only used if pre-configured)")]
        public Attribute attribute;
        
        [Tooltip("Display label for this stat (e.g., 'HP', 'Speed')")]
        public string displayLabel = "Stat";
        
        [Header("UI References")]
        [Tooltip("Label text (optional)")]
        public TMP_Text labelText;
        
        [Tooltip("Value text (required)")]
        public TMP_Text valueText;
        
        [Tooltip("Progress bar (optional - for HP, Energy, etc.)")]
        public Slider progressBar;
        
        [Tooltip("StatValueDisplay component (optional - for modifier tooltips)")]
        public StatValueDisplay statDisplay;
        
        // Runtime state
        private string runtimeStatName;
        private Entity currentEntity;
        private Attribute? runtimeAttribute;
        
        void Awake()
        {
            // Auto-find StatValueDisplay if not set
            if (statDisplay == null && valueText != null)
            {
                statDisplay = valueText.GetComponent<StatValueDisplay>();
            }
        }
        
        /// <summary>
        /// Configure this field for a specific stat (for runtime-created fields).
        /// </summary>
        public void Configure(string statName, string label)
        {
            runtimeStatName = statName;
            displayLabel = label;
            runtimeAttribute = null;
            
            if (labelText != null)
            {
                labelText.text = label;
            }
        }
        
        /// <summary>
        /// Configure this field with a DisplayStat (includes attribute for tooltip).
        /// </summary>
        public void Configure(VehicleComponent.DisplayStat stat)
        {
            runtimeStatName = stat.Name;
            displayLabel = stat.Label;
            runtimeAttribute = stat.Attribute;
            
            if (labelText != null)
            {
                labelText.text = stat.Label;
            }
        }
        
        /// <summary>
        /// Get the stat name this field represents.
        /// </summary>
        public string GetStatName()
        {
            return !string.IsNullOrEmpty(runtimeStatName) ? runtimeStatName : attribute.ToString();
        }
        
        /// <summary>
        /// Update this stat field with a DisplayStat.
        /// </summary>
        public void UpdateDisplay(Entity entity, VehicleComponent.DisplayStat stat, bool hasThisStat)
        {
            gameObject.SetActive(hasThisStat);
            
            if (!hasThisStat) return;
            
            currentEntity = entity;
            
            // Update label
            if (labelText != null)
            {
                labelText.text = stat.Label;
            }
            
            // Use StatValueDisplay if available and stat has attribute for tooltip
            if (statDisplay != null && entity != null && stat.Attribute.HasValue)
            {
                statDisplay.UpdateDisplay(
                    entity,
                    stat.Attribute.Value,
                    stat.BaseValue,
                    stat.FinalValue,
                    stat.Value
                );
            }
            else
            {
                if (valueText != null)
                {
                    valueText.text = stat.Value;
                }
                
                // Clear any stale tooltip data
                if (statDisplay != null)
                {
                    statDisplay.ClearTooltipData();
                }
            }
            
            // Update progress bar if present and stat has bar data
            if (progressBar != null && stat.ShowBar && stat.Max.HasValue && stat.Max.Value > 0)
            {
                float percent = Mathf.Clamp01(stat.Current.Value / stat.Max.Value);
                progressBar.value = percent;
            }
        }
        
        /// <summary>
        /// Update this stat field with current/max values and tooltip support.
        /// </summary>
        public void UpdateDisplay(Entity entity, float currentValue, float maxValue, bool hasThisStat)
        {
            gameObject.SetActive(hasThisStat);
            
            if (!hasThisStat) return;
            
            currentEntity = entity;
            
            // Update label
            if (labelText != null)
            {
                labelText.text = displayLabel;
            }
            
            // Determine display text
            string displayText = maxValue > 0 
                ? $"{currentValue:F0}/{maxValue:F0}" 
                : $"{currentValue:F1}";
            
            // Use StatValueDisplay if available for tooltip support
            if (statDisplay != null && entity != null && runtimeAttribute.HasValue)
            {
                float baseValue = maxValue > 0 ? maxValue : currentValue;
                float finalValue = maxValue > 0 ? maxValue : currentValue;
                
                statDisplay.UpdateDisplay(
                    entity,
                    runtimeAttribute.Value,
                    baseValue,
                    finalValue,
                    displayText
                );
            }
            else if (valueText != null)
            {
                valueText.text = displayText;
                
                // Clear any stale tooltip data
                if (statDisplay != null)
                {
                    statDisplay.ClearTooltipData();
                }
            }
            
            // Update progress bar if present
            if (progressBar != null && maxValue > 0)
            {
                float percent = Mathf.Clamp01(currentValue / maxValue);
                progressBar.value = percent;
            }
        }
        
        /// <summary>
        /// Update display for stat without a max value (Speed, AC, etc.)
        /// </summary>
        public void UpdateDisplay(Entity entity, float value, bool hasThisStat)
        {
            UpdateDisplay(entity, value, 0, hasThisStat);
        }
        
        /// <summary>
        /// Simple update with just a value string (no tooltip).
        /// </summary>
        public void UpdateDisplay(string valueString, bool hasThisStat)
        {
            gameObject.SetActive(hasThisStat);
            
            if (!hasThisStat) return;
            
            if (labelText != null)
            {
                labelText.text = displayLabel;
            }
            
            // Clear tooltip data since we don't have entity info
            if (statDisplay != null)
            {
                statDisplay.ClearTooltipData();
            }
            
            if (valueText != null)
            {
                valueText.text = valueString;
            }
        }
        
        /// <summary>
        /// Update with entity for tooltip support and value string.
        /// </summary>
        public void UpdateDisplay(Entity entity, string valueString, float baseValue, float finalValue, bool hasThisStat)
        {
            gameObject.SetActive(hasThisStat);
            
            if (!hasThisStat) return;
            
            currentEntity = entity;
            
            if (labelText != null)
            {
                labelText.text = displayLabel;
            }
            
            // Use StatValueDisplay for tooltip support only if we have a valid attribute
            if (statDisplay != null && entity != null && runtimeAttribute.HasValue)
            {
                statDisplay.UpdateDisplay(entity, runtimeAttribute.Value, baseValue, finalValue, valueString);
            }
            else
            {
                if (valueText != null)
                {
                    valueText.text = valueString;
                }
                
                // Clear any stale tooltip data
                if (statDisplay != null)
                {
                    statDisplay.ClearTooltipData();
                }
            }
        }
    }
}
