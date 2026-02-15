using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace Assets.Scripts.UI.Components
{
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
        
        private string runtimeStatName;
        private Entity currentEntity;
        private Attribute? runtimeAttribute;

        void Awake()
        {
            if (statDisplay == null && valueText != null)
                statDisplay = valueText.GetComponent<StatValueDisplay>();
        }
        
        public void Configure(string statName, string label)
        {
            runtimeStatName = statName;
            displayLabel = label;
            runtimeAttribute = null;

            if (labelText != null)
                labelText.text = label;
        }

        public void Configure(VehicleComponentUI.DisplayStat stat)
        {
            runtimeStatName = stat.Name;
            displayLabel = stat.Label;
            runtimeAttribute = stat.Attribute;

            if (labelText != null)
                labelText.text = stat.Label;
        }

        public string GetStatName()
        {
            return !string.IsNullOrEmpty(runtimeStatName) ? runtimeStatName : attribute.ToString();
        }
        
        public void UpdateDisplay(Entity entity, VehicleComponentUI.DisplayStat stat, bool hasThisStat)
        {
            gameObject.SetActive(hasThisStat);

            if (!hasThisStat) return;

            currentEntity = entity;

            if (labelText != null)
                labelText.text = stat.Label;

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
                    valueText.text = stat.Value;

                if (statDisplay != null)
                    statDisplay.ClearTooltipData();
            }

            if (progressBar != null && stat.ShowBar && stat.Max.HasValue && stat.Max.Value > 0)
            {
                float percent = Mathf.Clamp01(stat.Current.Value / stat.Max.Value);
                progressBar.value = percent;
            }
        }
        
        public void UpdateDisplay(Entity entity, float currentValue, float maxValue, bool hasThisStat)
        {
            gameObject.SetActive(hasThisStat);

            if (!hasThisStat) return;

            currentEntity = entity;

            if (labelText != null)
                labelText.text = displayLabel;

            string displayText = maxValue > 0 
                ? $"{currentValue:F0}/{maxValue:F0}" 
                : $"{currentValue:F0}";

            if (statDisplay != null && entity != null && runtimeAttribute.HasValue)
            {
                int baseValue = maxValue > 0 ? Mathf.RoundToInt(maxValue) : Mathf.RoundToInt(currentValue);
                int finalValue = maxValue > 0 ? Mathf.RoundToInt(maxValue) : Mathf.RoundToInt(currentValue);
                
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

                if (statDisplay != null)
                    statDisplay.ClearTooltipData();
            }

            if (progressBar != null && maxValue > 0)
            {
                float percent = Mathf.Clamp01(currentValue / maxValue);
                progressBar.value = percent;
            }
        }
        
        public void UpdateDisplay(Entity entity, float value, bool hasThisStat)
        {
            UpdateDisplay(entity, value, 0, hasThisStat);
        }

        public void UpdateDisplay(string valueString, bool hasThisStat)
        {
            gameObject.SetActive(hasThisStat);

            if (!hasThisStat) return;

            if (labelText != null)
                labelText.text = displayLabel;

            if (statDisplay != null)
                statDisplay.ClearTooltipData();

            if (valueText != null)
                valueText.text = valueString;
        }

        public void UpdateDisplay(Entity entity, string valueString, int baseValue, int finalValue, bool hasThisStat)
        {
            gameObject.SetActive(hasThisStat);
            
            if (!hasThisStat) return;
            
            currentEntity = entity;
            
            if (labelText != null)
                labelText.text = displayLabel;

            if (statDisplay != null && entity != null && runtimeAttribute.HasValue)
            {
                statDisplay.UpdateDisplay(entity, runtimeAttribute.Value, baseValue, finalValue, valueString);
            }
            else
            {
                if (valueText != null)
                    valueText.text = valueString;

                if (statDisplay != null)
                    statDisplay.ClearTooltipData();
            }
        }
    }
}
