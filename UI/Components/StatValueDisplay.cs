using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using Assets.Scripts.Combat.Logging;
using Assets.Scripts.Core;
using Assets.Scripts.Entities;
using Assets.Scripts.UI.Tabs.EventFeed;

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
        private EntityAttribute tooltipAttribute;
        
        void Awake()
        {
            textComponent = GetComponent<TextMeshProUGUI>();
            rectTransform = GetComponent<RectTransform>();
        }
        
        public void UpdateDisplay(
            Entity entity,
            EntityAttribute attribute,
            int baseValue,
            int finalValue,
            string displayText = null)
        {
            if (textComponent == null) return;

            tooltipEntity = entity;
            tooltipAttribute = attribute;

            textComponent.text = displayText ?? finalValue.ToString();

            int totalModifiers = finalValue - baseValue;
            ApplyModifierColor(totalModifiers);
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
            ApplyModifierColor(totalModifiers);

            tooltipEntity = null;
        }

        private void ApplyModifierColor(float delta)
        {
            if (Mathf.Approximately(delta, 0f))
                textComponent.color = normalColor;
            else if (delta > 0)
                textComponent.color = buffedColor;
            else
                textComponent.color = debuffedColor;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (tooltipEntity == null) return;

            var (total, baseValue, modifiers) = StatCalculator.GatherAttributeValueWithBreakdown(
                tooltipEntity, tooltipAttribute);

            string tooltipContent = CombatFormatter.FormatStatBreakdown(
                tooltipAttribute, baseValue, total, modifiers);

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
