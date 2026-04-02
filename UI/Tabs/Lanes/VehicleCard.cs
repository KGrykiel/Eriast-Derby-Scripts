using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Assets.Scripts.Entities.Vehicles;

namespace Assets.Scripts.UI.Tabs.Lanes
{
    public class VehicleCard : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private Slider hpBar;
        [SerializeField] private Button cardButton;
        
        private Vehicle vehicle;
        
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

        public void Refresh()
        {
            if (vehicle == null) return;

            if (nameText != null)
                nameText.text = vehicle.vehicleName;

            if (hpBar != null && vehicle.Chassis != null)
            {
                int currentHP = vehicle.Chassis.GetCurrentHealth();
                int maxHP = vehicle.Chassis.GetMaxHealth();
                hpBar.value = maxHP > 0 ? (float)currentHP / maxHP : 0f;
            }
        }
        
        private void OnCardClicked()
        {
            if (vehicle == null) return;

            var tabManager = FindFirstObjectByType<TabManager>();
            if (tabManager != null && tabManager.inspectorTabButton != null)
                tabManager.inspectorTabButton.onClick.Invoke();

            var inspectorPanel = FindFirstObjectByType<VehicleInspectorPanel>();
            if (inspectorPanel != null)
                inspectorPanel.SelectVehicle(vehicle);
        }
    }
}
