using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using Assets.Scripts.Entities.Vehicle;

namespace Assets.Scripts.UI.Components
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class SizeDisplay : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private TextMeshProUGUI textComponent;
        private RectTransform rectTransform;

        private VehicleSizeCategory currentSize;
        private string cachedTooltip;

        void Awake()
        {
            textComponent = GetComponent<TextMeshProUGUI>();
            rectTransform = GetComponent<RectTransform>();
        }

        public void UpdateDisplay(VehicleSizeCategory size, Color color, string tooltip)
        {
            if (textComponent == null) textComponent = GetComponent<TextMeshProUGUI>();
            if (rectTransform == null) rectTransform = GetComponent<RectTransform>();

            if (textComponent == null) return;

            currentSize = size;
            cachedTooltip = tooltip;

            textComponent.text = size.ToString();
            textComponent.color = color;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (string.IsNullOrEmpty(cachedTooltip)) return;
            
            RollTooltip.ShowNow(cachedTooltip, rectTransform);
        }
        
        public void OnPointerExit(PointerEventData eventData)
        {
            RollTooltip.Hide();
        }
    }
}
