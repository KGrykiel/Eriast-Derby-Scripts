using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Assets.Scripts.Stages;
using Assets.Scripts.Logging;

namespace Assets.Scripts.UI.Tabs.Lanes
{
    /// <summary>
    /// Main controller for the Lane View UI tab.
    /// Shows lanes as vertical columns with vehicle cards positioned by progress.
    /// Observes game state and refreshes when changes detected (like other panels).
    /// </summary>
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
        
        // Dirty tracking (like VehicleInspectorPanel)
        private int lastEventCount = 0;
        private int lastVehicleHash = 0;
        
        private void Start()
        {
            // Setup UI events
            if (refreshButton != null)
            {
                refreshButton.onClick.AddListener(RefreshView);
            }
            
            if (stageDropdown != null)
            {
                stageDropdown.onValueChanged.AddListener(OnStageSelected);
            }
            
            // Initial setup
            PopulateStageDropdown();
            SelectDefaultStage();
        }
        
        private void Update()
        {
            // Only check for changes when panel is active
            if (!gameObject.activeInHierarchy) return;
            
            // Check if state has changed (polling like other panels)
            if (HasStateChanged())
            {
                RefreshView();
            }
        }
        
        private void OnEnable()
        {
            // Refresh when tab becomes visible
            PopulateStageDropdown();
            SelectDefaultStage();
            RefreshView();
        }
        
        /// <summary>
        /// Check if game state has changed since last refresh.
        /// </summary>
        private bool HasStateChanged()
        {
            // Check event count (new events = something happened)
            if (RaceHistory.Instance != null)
            {
                int currentEventCount = RaceHistory.Instance.AllEvents.Count;
                if (currentEventCount != lastEventCount)
                {
                    lastEventCount = currentEventCount;
                    return true;
                }
            }
            
            // Check vehicle positions hash (lane changes, progress changes)
            int currentHash = CalculateVehicleHash();
            if (currentHash != lastVehicleHash)
            {
                lastVehicleHash = currentHash;
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// Calculate a hash of vehicle positions for change detection.
        /// </summary>
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
        
        /// <summary>
        /// Populate the stage dropdown with all stages that have lanes.
        /// </summary>
        private void PopulateStageDropdown()
        {
            if (stageDropdown == null) return;
            
            stagesWithLanes.Clear();
            stageDropdown.ClearOptions();
            
            // Find all stages in scene
            var allStages = FindObjectsByType<Stage>(FindObjectsSortMode.None);
            var options = new List<string>();
            
            foreach (var stage in allStages)
            {
                // Include all stages, but mark those with lanes
                stagesWithLanes.Add(stage);
                
                string label = stage.lanes != null && stage.lanes.Count > 0 
                    ? $"{stage.stageName} ({stage.lanes.Count} lanes)"
                    : $"{stage.stageName} (no lanes)";
                
                options.Add(label);
            }
            
            stageDropdown.AddOptions(options);
        }
        
        /// <summary>
        /// Select the default stage (player's current stage or first with lanes).
        /// </summary>
        private void SelectDefaultStage()
        {
            if (stagesWithLanes.Count == 0) return;
            
            // Try to find player's current stage via GameManager
            var gameManager = FindFirstObjectByType<GameManager>();
            if (gameManager != null)
            {
                var vehicles = gameManager.GetVehicles();
                if (vehicles != null)
                {
                    // Find player vehicle
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
            
            // Fallback: First stage with lanes
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
            
            // Final fallback: First stage
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
            lastVehicleHash = 0; // Force refresh
            RefreshView();
        }
        
        /// <summary>
        /// Refresh the entire lane view.
        /// </summary>
        public void RefreshView()
        {
            // Update dirty tracking
            if (RaceHistory.Instance != null)
            {
                lastEventCount = RaceHistory.Instance.AllEvents.Count;
            }
            lastVehicleHash = CalculateVehicleHash();
            
            // Clear existing columns
            foreach (var column in spawnedColumns)
            {
                if (column != null)
                    Destroy(column.gameObject);
            }
            spawnedColumns.Clear();
            
            // Validate we can spawn columns
            if (currentStage == null || currentStage.lanes == null || currentStage.lanes.Count == 0)
                return;
            
            if (laneColumnPrefab == null || laneContainer == null)
                return;
            
            // Spawn lane columns
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
        
        /// <summary>
        /// Set the displayed stage externally.
        /// </summary>
        public void ShowStage(Stage stage)
        {
            if (stage == null) return;
            
            // Update dropdown selection
            int index = stagesWithLanes.IndexOf(stage);
            if (index >= 0 && stageDropdown != null)
            {
                stageDropdown.value = index;
            }
            
            SetCurrentStage(stage);
        }
    }
}
