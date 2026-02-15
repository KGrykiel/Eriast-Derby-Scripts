using UnityEngine;
using UnityEngine.UI;
using TMPro;

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

            var tabManager = FindFirstObjectByType<TabManager>();
            if (tabManager != null && tabManager.inspectorTabButton != null)
                tabManager.inspectorTabButton.onClick.Invoke();

            var inspectorPanel = FindFirstObjectByType<VehicleInspectorPanel>();
            if (inspectorPanel != null)
                inspectorPanel.SelectVehicle(vehicle);
        }
    }
}
