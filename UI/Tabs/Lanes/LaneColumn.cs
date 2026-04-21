using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Assets.Scripts.Stages;
using Assets.Scripts.Stages.Lanes;
using Assets.Scripts.Managers.Race;

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
        [SerializeField] private Color defaultColor = new(0.3f, 0.3f, 0.3f, 1f);
        [SerializeField] private Color fastColor = new(0.9f, 0.7f, 0.2f, 1f);
        [SerializeField] private Color safeColor = new(0.3f, 0.5f, 0.8f, 1f);
        [SerializeField] private Color hazardousColor = new(0.8f, 0.2f, 0.2f, 1f);
        
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
                int count = RacePositionTracker.GetVehiclesInLane(lane).Count;
                vehicleCountText.text = count > 0 ? $"({count})" : "";
            }

            if (headerBackground != null)
                headerBackground.color = GetLaneColor();
        }
        
        private string GetLaneInfoText()
        {
            var parts = new List<string>();

            if (lane.turnEffects != null && lane.turnEffects.Count > 0)
                parts.Add($"[!]{lane.turnEffects.Count}");

            if (TrackDefinition.Active != null)
            {
                Stage nextStage = TrackDefinition.Active.GetNextStage(lane);
                if (nextStage != null)
                    parts.Add($"->{nextStage.stageName}");
            }

            return parts.Count > 0 ? string.Join(" ", parts) : "-";
        }
        
        private Color GetLaneColor()
        {
            bool hasDanger = lane.turnEffects != null && lane.turnEffects.Count > 0;

            if (hasDanger)
                return hazardousColor;

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

            if (vehicleCardPrefab == null || vehicleContainer == null)
                return;

            var sortedVehicles = RacePositionTracker.GetVehiclesInLane(lane)
                .Where(v => v != null)
                .OrderByDescending(v => RacePositionTracker.GetProgress(v))
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
