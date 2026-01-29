using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using Assets.Scripts.Entities.Vehicle;

namespace Assets.Scripts.UI.Components
{
    /// <summary>
    /// UI component for displaying vehicle size with color coding and tooltip.
    /// Hover shows size modifier breakdown.
    /// Follows the same pattern as StatValueDisplay.
    /// </summary>
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class SizeDisplay : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private TextMeshProUGUI textComponent;
        private RectTransform rectTransform;
        
        // Tooltip data
        private VehicleSizeCategory currentSize;
        private string cachedTooltip;
        
        void Awake()
        {
            textComponent = GetComponent<TextMeshProUGUI>();
            rectTransform = GetComponent<RectTransform>();
        }
        
        /// <summary>
        /// Update the displayed size with color coding and tooltip.
        /// </summary>
        public void UpdateDisplay(VehicleSizeCategory size, Color color, string tooltip)
        {
            // Ensure components are initialized (in case Awake hasn't been called yet)
            if (textComponent == null) textComponent = GetComponent<TextMeshProUGUI>();
            if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
            
            if (textComponent == null) return;
            
            currentSize = size;
            cachedTooltip = tooltip;
            
            textComponent.text = size.ToString();
            textComponent.color = color;
        }
        
        // ==================== HOVER TOOLTIP ====================
        
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (string.IsNullOrEmpty(cachedTooltip)) return;
            
            // Show tooltip positioned near this text (use ShowNow like StatValueDisplay)
            RollTooltip.ShowNow(cachedTooltip, rectTransform);
        }
        
        public void OnPointerExit(PointerEventData eventData)
        {
            RollTooltip.Hide();
        }
    }
}
