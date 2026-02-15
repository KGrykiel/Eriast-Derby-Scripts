using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using Assets.Scripts.Combat.Logging;

namespace Assets.Scripts.UI.Components
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class StatValueDisplay : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("Colors")]
        [Tooltip("Color when stat is buffed (positive modifiers)")]
        public Color buffedColor = new(0.27f, 1f, 0.27f); // #44FF44
        
        [Tooltip("Color when stat is debuffed (negative modifiers)")]
        public Color debuffedColor = new(1f, 0.27f, 0.27f); // #FF4444
        
        [Tooltip("Color when stat is at base value (no modifiers)")]
        public Color normalColor = Color.white;
        
        private TextMeshProUGUI textComponent;
        private RectTransform rectTransform;

        private Entity tooltipEntity;
        private Attribute tooltipAttribute;
        private int tooltipBaseValue;
        private int tooltipFinalValue;
        
        void Awake()
        {
            textComponent = GetComponent<TextMeshProUGUI>();
            rectTransform = GetComponent<RectTransform>();
        }
        
        public void UpdateDisplay(
            Entity entity,
            Attribute attribute,
            int baseValue,
            int finalValue,
            string displayText = null)
        {
            if (textComponent == null) return;

            tooltipEntity = entity;
            tooltipAttribute = attribute;
            tooltipBaseValue = baseValue;
            tooltipFinalValue = finalValue;

            textComponent.text = displayText ?? finalValue.ToString();

            int totalModifiers = finalValue - baseValue;
            
            if (totalModifiers == 0)
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
        
        public void UpdateDisplay(float value, string displayText = null)
        {
            if (textComponent == null) return;

            textComponent.text = displayText ?? value.ToString("F1");
            textComponent.color = normalColor;
            ClearTooltipData();
        }

        public void ClearTooltipData()
        {
            tooltipEntity = null;
        }

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
            
            tooltipEntity = null;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (tooltipEntity == null) return;

            string tooltipContent = CombatFormatter.FormatStatBreakdown(
                tooltipEntity,
                tooltipAttribute,
                tooltipBaseValue,
                tooltipFinalValue);

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
