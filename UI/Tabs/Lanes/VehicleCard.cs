using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Assets.Scripts.UI.Tabs.Lanes
{
    /// <summary>
    /// Minimal vehicle card for lane view.
    /// Shows vehicle name and HP bar only.
    /// Compact design to fit many vehicles per lane.
    /// Size and colors determined by prefab settings only.
    /// </summary>
    public class VehicleCard : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private Slider hpBar;
        [SerializeField] private Button cardButton;
        
        private Vehicle vehicle;
        
        /// <summary>
        /// Initialize the card with vehicle data.
        /// </summary>
        public void Initialize(Vehicle vehicle)
        {
            this.vehicle = vehicle;
            
            if (cardButton != null)
            {
                cardButton.onClick.RemoveAllListeners();
                cardButton.onClick.AddListener(OnCardClicked);
            }
            
            Refresh();
        }
        
        /// <summary>
        /// Refresh the card display with current vehicle data.
        /// </summary>
        public void Refresh()
        {
            if (vehicle == null) return;
            
            // Name
            if (nameText != null)
            {
                nameText.text = vehicle.vehicleName;
            }
            
            // HP bar value only (color set in prefab)
            if (hpBar != null && vehicle.chassis != null)
            {
                int currentHP = vehicle.chassis.GetCurrentHealth();
                int maxHP = vehicle.chassis.GetMaxHealth();
                hpBar.value = maxHP > 0 ? (float)currentHP / maxHP : 0f;
            }
        }
        
        private void OnCardClicked()
        {
            if (vehicle == null) return;
            
            // Switch to Inspector tab FIRST (so panel is active)
            var tabManager = FindFirstObjectByType<TabManager>();
            if (tabManager != null && tabManager.inspectorTabButton != null)
            {
                tabManager.inspectorTabButton.onClick.Invoke();
            }
            
            // Find and select vehicle in inspector panel
            var inspectorPanel = FindFirstObjectByType<VehicleInspectorPanel>();
            if (inspectorPanel != null)
            {
                inspectorPanel.SelectVehicle(vehicle);
            }
        }
    }
}
