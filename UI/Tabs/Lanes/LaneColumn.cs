using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Assets.Scripts.Stages;
using Assets.Scripts.Stages.Lanes;

namespace Assets.Scripts.UI.Tabs.Lanes
{
    public class LaneColumn : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI laneNameText;
        [SerializeField] private TextMeshProUGUI laneInfoText;
        [SerializeField] private TextMeshProUGUI vehicleCountText;
        [SerializeField] private Image headerBackground;
        [SerializeField] private RectTransform vehicleContainer;
        
        [Header("Prefabs")]
        [SerializeField] private GameObject vehicleCardPrefab;
        
        [Header("Lane Type Colors")]
        [SerializeField] private Color defaultColor = new Color(0.3f, 0.3f, 0.3f, 1f);
        [SerializeField] private Color fastColor = new Color(0.9f, 0.7f, 0.2f, 1f);
        [SerializeField] private Color safeColor = new Color(0.3f, 0.5f, 0.8f, 1f);
        [SerializeField] private Color hazardousColor = new Color(0.8f, 0.2f, 0.2f, 1f);
        
        private StageLane lane;
        private Stage stage;
        private int laneIndex;
        private List<VehicleCard> spawnedCards = new();
        
        public void Initialize(StageLane lane, int laneIndex, Stage stage)
        {
            this.lane = lane;
            this.laneIndex = laneIndex;
            this.stage = stage;
            Refresh();
        }

        public void Refresh()
        {
            if (lane == null) return;
            
            UpdateHeader();
            UpdateVehicleCards();
        }
        
        
        private void UpdateHeader()
        {
            if (laneNameText != null)
                laneNameText.text = $"Lane {laneIndex}: {lane.laneName}";

            if (laneInfoText != null)
                laneInfoText.text = GetLaneInfoText();

            if (vehicleCountText != null)
            {
                int count = lane.vehiclesInLane?.Count ?? 0;
                vehicleCountText.text = count > 0 ? $"({count})" : "";
            }

            if (headerBackground != null)
                headerBackground.color = GetLaneColor();
        }
        
        private string GetLaneInfoText()
        {
            var parts = new List<string>();

            if (lane.laneStatusEffect != null)
            {
                if (lane.laneStatusEffect.modifiers != null)
                {
                    foreach (var mod in lane.laneStatusEffect.modifiers)
                    {
                        string sign = mod.value >= 0 ? "+" : "";
                        string attrShort = mod.attribute switch
                        {
                            Attribute.ArmorClass => "AC",
                            Attribute.MaxSpeed => "Spd",
                            Attribute.MaxHealth => "HP",
                            _ => mod.attribute.ToString().Substring(0, 3)
                        };
                        parts.Add($"{attrShort}{sign}{mod.value}");
                    }
                }
            }
            
            if (lane.turnEffects != null && lane.turnEffects.Count > 0)
                parts.Add($"⚠{lane.turnEffects.Count}");

            if (lane.nextStage != null)
                parts.Add($"→{lane.nextStage.stageName}");
            
            return parts.Count > 0 ? string.Join(" ", parts) : "-";
        }
        
        private Color GetLaneColor()
        {
            if (lane.laneStatusEffect == null && (lane.turnEffects == null || lane.turnEffects.Count == 0))
                return defaultColor;

            bool hasDanger = lane.turnEffects != null && lane.turnEffects.Count > 0;
            bool hasSpeedBoost = false;
            bool hasDefenseBoost = false;
            
            if (lane.laneStatusEffect?.modifiers != null)
            {
                foreach (var mod in lane.laneStatusEffect.modifiers)
                {
                    if (mod.attribute == Attribute.MaxSpeed && mod.value > 0)
                        hasSpeedBoost = true;
                    if (mod.attribute == Attribute.ArmorClass && mod.value > 0)
                        hasDefenseBoost = true;
                }
            }
            
            if (hasDanger)
                return hazardousColor;
            if (hasSpeedBoost)
                return fastColor;
            if (hasDefenseBoost)
                return safeColor;
            
            return defaultColor;
        }
        
        private void UpdateVehicleCards()
        {
            foreach (var card in spawnedCards)
            {
                if (card != null)
                    Destroy(card.gameObject);
            }
            spawnedCards.Clear();

            if (lane.vehiclesInLane == null || vehicleCardPrefab == null || vehicleContainer == null)
                return;

            var sortedVehicles = lane.vehiclesInLane
                .Where(v => v != null)
                .OrderByDescending(v => v.progress)
                .ToList();

            foreach (var vehicle in sortedVehicles)
            {
                var cardObj = Instantiate(vehicleCardPrefab, vehicleContainer);
                var card = cardObj.GetComponent<VehicleCard>();
                
                if (card != null)
                {
                    card.Initialize(vehicle);
                    spawnedCards.Add(card);
                }
            }
        }
    }
}
