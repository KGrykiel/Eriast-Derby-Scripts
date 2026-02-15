using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Assets.Scripts.Stages;
using Assets.Scripts.Logging;

namespace Assets.Scripts.UI.Tabs.Lanes
{
    public class LaneViewPanel : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TMP_Dropdown stageDropdown;
        [SerializeField] private Button refreshButton;
        [SerializeField] private Transform laneContainer;
        
        [Header("Prefabs")]
        [SerializeField] private GameObject laneColumnPrefab;
        
        private Stage currentStage;
        private List<Stage> stagesWithLanes = new();
        private List<LaneColumn> spawnedColumns = new();
        
        private int lastEventCount = 0;
        private int lastVehicleHash = 0;

        private void Start()
        {
            if (refreshButton != null)
                refreshButton.onClick.AddListener(RefreshView);

            if (stageDropdown != null)
                stageDropdown.onValueChanged.AddListener(OnStageSelected);

            PopulateStageDropdown();
            SelectDefaultStage();
        }

        private void Update()
        {
            if (!gameObject.activeInHierarchy) return;

            if (HasStateChanged())
                RefreshView();
        }

        private void OnEnable()
        {
            PopulateStageDropdown();
            SelectDefaultStage();
            RefreshView();
        }
        
        private bool HasStateChanged()
        {
            if (RaceHistory.Instance != null)
            {
                int currentEventCount = RaceHistory.Instance.AllEvents.Count;
                if (currentEventCount != lastEventCount)
                {
                    lastEventCount = currentEventCount;
                    return true;
                }
            }

            int currentHash = CalculateVehicleHash();
            if (currentHash != lastVehicleHash)
            {
                lastVehicleHash = currentHash;
                return true;
            }

            return false;
        }

        private int CalculateVehicleHash()
        {
            if (currentStage == null) return 0;
            
            int hash = 17;
            foreach (var vehicle in currentStage.vehiclesInStage)
            {
                if (vehicle == null) continue;
                hash = hash * 31 + vehicle.GetInstanceID();
                hash = hash * 31 + vehicle.progress;
                hash = hash * 31 + (vehicle.currentLane?.GetInstanceID() ?? 0);
            }
            return hash;
        }
        
        private void PopulateStageDropdown()
        {
            if (stageDropdown == null) return;

            stagesWithLanes.Clear();
            stageDropdown.ClearOptions();

            var allStages = FindObjectsByType<Stage>(FindObjectsSortMode.None);
            var options = new List<string>();

            foreach (var stage in allStages)
            {
                stagesWithLanes.Add(stage);

                string label = stage.lanes != null && stage.lanes.Count > 0 
                    ? $"{stage.stageName} ({stage.lanes.Count} lanes)"
                    : $"{stage.stageName} (no lanes)";

                options.Add(label);
            }

            stageDropdown.AddOptions(options);
        }

        private void SelectDefaultStage()
        {
            if (stagesWithLanes.Count == 0) return;

            var gameManager = FindFirstObjectByType<GameManager>();
            if (gameManager != null)
            {
                var vehicles = gameManager.GetVehicles();
                if (vehicles != null)
                {
                    foreach (var vehicle in vehicles)
                    {
                        if (vehicle != null && vehicle.controlType == ControlType.Player && vehicle.currentStage != null)
                        {
                            int index = stagesWithLanes.IndexOf(vehicle.currentStage);
                            if (index >= 0)
                            {
                                stageDropdown.value = index;
                                SetCurrentStage(vehicle.currentStage);
                                return;
                            }
                        }
                    }
                }
            }
            
            foreach (var stage in stagesWithLanes)
            {
                if (stage.lanes != null && stage.lanes.Count > 0)
                {
                    int index = stagesWithLanes.IndexOf(stage);
                    stageDropdown.value = index;
                    SetCurrentStage(stage);
                    return;
                }
            }
            
            if (stagesWithLanes.Count > 0)
            {
                stageDropdown.value = 0;
                SetCurrentStage(stagesWithLanes[0]);
            }
        }
        
        private void OnStageSelected(int index)
        {
            if (index >= 0 && index < stagesWithLanes.Count)
            {
                SetCurrentStage(stagesWithLanes[index]);
            }
        }
        
        private void SetCurrentStage(Stage stage)
        {
            currentStage = stage;
            lastVehicleHash = 0;
            RefreshView();
        }

        public void RefreshView()
        {
            if (RaceHistory.Instance != null)
                lastEventCount = RaceHistory.Instance.AllEvents.Count;
            lastVehicleHash = CalculateVehicleHash();

            foreach (var column in spawnedColumns)
            {
                if (column != null)
                    Destroy(column.gameObject);
            }
            spawnedColumns.Clear();

            if (currentStage == null || currentStage.lanes == null || currentStage.lanes.Count == 0)
                return;

            if (laneColumnPrefab == null || laneContainer == null)
                return;

            for (int i = 0; i < currentStage.lanes.Count; i++)
            {
                var lane = currentStage.lanes[i];
                if (lane == null) continue;

                var columnObj = Instantiate(laneColumnPrefab, laneContainer);
                var column = columnObj.GetComponent<LaneColumn>();

                if (column != null)
                {
                    column.Initialize(lane, i, currentStage);
                    spawnedColumns.Add(column);
                }
            }
        }

        public void ShowStage(Stage stage)
        {
            if (stage == null) return;
            
            int index = stagesWithLanes.IndexOf(stage);
            if (index >= 0 && stageDropdown != null)
            {
                stageDropdown.value = index;
            }
            
            SetCurrentStage(stage);
        }
    }
}
