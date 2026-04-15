using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using Assets.Scripts.Entities.Vehicles;
using Assets.Scripts.Managers;
using Assets.Scripts.Stages;
using Assets.Scripts.Stages.Lanes;
using Assets.Scripts.UI.Tabs.Lanes;

namespace Assets.Scripts.Visualisation
{
    /// <summary>
    /// Bootstrap MonoBehaviour. On Start, adds StageVisual and LaneVisual components to all
    /// registered Stage and StageLane GameObjects, wires procedural fallback endpoints, and
    /// triggers initial route line rendering.
    /// Place on any active GameObject in the scene.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public class TrackVisualizationManager : MonoBehaviour
    {
        [Header("Visuals")]
        [Tooltip("Optional material for route LineRenderers. Defaults to Unity's built-in line material if unassigned.")]
        [SerializeField] private Material routeLineMaterial;

        [Tooltip("Optional shared base material for vehicle meshes. Each vehicle receives its own colour-coded instance at runtime.")]
        [SerializeField] private Material vehicleBaseMaterial;

        [Header("Interaction")]
        [Tooltip("Camera with CameraController3D. Required for click-to-focus.")]
        [SerializeField] private CameraController3D cameraController;

        [Tooltip("TabManager in the scene. Required for vehicle clicks to open the Inspector tab.")]
        [SerializeField] private TabManager tabManager;

        [Tooltip("VehicleInspectorPanel in the scene. Required for vehicle clicks to populate the inspector.")]
        [SerializeField] private VehicleInspectorPanel vehicleInspectorPanel;

        [Tooltip("LaneViewPanel in the scene. Required for stage clicks to open the Lanes tab.")]
        [SerializeField] private LaneViewPanel laneViewPanel;

        // ==================== STATIC CLICK EVENTS ====================

        private static event Action<Vehicle> VehicleClicked;
        private static event Action<Stage>   StageClicked;

        /// <summary>Called by VehicleVisual.OnMouseDown to route a vehicle click to this manager.</summary>
        internal static void RaiseVehicleClicked(Vehicle vehicle) => VehicleClicked?.Invoke(vehicle);

        /// <summary>Called by StageVisual.OnMouseDown to route a stage click to this manager.</summary>
        internal static void RaiseStageClicked(Stage stage) => StageClicked?.Invoke(stage);

        private void Start()
        {
            if (TrackDefinition.Active == null)
            {
                Debug.LogWarning("[TrackVisualizationManager] TrackDefinition.Active is null — visuals will not initialise. Ensure GameManager runs before this component.");
                return;
            }

            InitialiseVisuals();

            VehicleClicked += HandleVehicleClicked;
            StageClicked   += HandleStageClicked;
        }

        private void OnDestroy()
        {
            VehicleClicked -= HandleVehicleClicked;
            StageClicked   -= HandleStageClicked;
        }

        private void Update()
        {
            if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame)
                return;

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            Camera cam = Camera.main;
            if (cam == null)
                return;

            Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
            RaycastHit[] hits = Physics.RaycastAll(ray);

            // Click what you see — closest collider wins.
            RaycastHit closest = default;
            float closestDist = float.MaxValue;
            bool found = false;

            foreach (RaycastHit hit in hits)
            {
                bool isClickable = hit.collider.GetComponentInParent<VehicleVisual>() != null
                                || hit.collider.GetComponentInParent<StageVisual>()  != null;

                if (isClickable && hit.distance < closestDist)
                {
                    closestDist = hit.distance;
                    closest     = hit;
                    found       = true;
                }
            }

            if (!found)
                return;

            VehicleVisual vv = closest.collider.GetComponentInParent<VehicleVisual>();
            if (vv != null)
            {
                RaiseVehicleClicked(closest.collider.GetComponentInParent<Vehicle>());
                return;
            }

            StageVisual sv = closest.collider.GetComponentInParent<StageVisual>();
            if (sv != null)
                RaiseStageClicked(closest.collider.GetComponentInParent<Stage>());
        }

        // ==================== CLICK HANDLERS ====================

        private void HandleVehicleClicked(Vehicle vehicle)
        {
            if (cameraController != null)
                cameraController.FocusOn(vehicle);

            if (UIManager.Instance != null)
                UIManager.Instance.ShowDMInterface();

            if (tabManager != null)
                tabManager.ShowInspectorTab();

            if (vehicleInspectorPanel != null)
                vehicleInspectorPanel.SelectVehicle(vehicle);
        }

        private void HandleStageClicked(Stage stage)
        {
            if (cameraController != null)
                cameraController.FocusOn(stage);

            if (UIManager.Instance != null)
                UIManager.Instance.ShowDMInterface();

            if (tabManager != null)
                tabManager.ShowLanesTab();

            if (laneViewPanel != null)
                laneViewPanel.ShowStage(stage);
        }

        // ==================== PRIVATE ====================

        private void InitialiseVisuals()
        {
            var stages = TrackDefinition.GetAll();
            var stageVisuals = new Dictionary<Stage, StageVisual>();

            // Pass 1: collect StageVisuals and verify LaneVisuals are present
            foreach (Stage stage in stages)
            {
                if (stage == null)
                    continue;

                StageVisual sv = stage.gameObject.GetComponent<StageVisual>();
                if (sv == null)
                {
                    Debug.LogWarning($"[TrackVisualizationManager] Stage '{stage.stageName}' is missing a StageVisual component. Regenerate stage prefabs via Assets/Racing/Regenerate All Stages.");
                    continue;
                }

                stageVisuals[stage] = sv;

                foreach (StageLane lane in stage.lanes)
                {
                    if (lane == null)
                        continue;

                    if (lane.gameObject.GetComponent<LaneVisual>() == null)
                        Debug.LogWarning($"[TrackVisualizationManager] Lane '{lane.laneName}' on stage '{stage.stageName}' is missing a LaneVisual component. Regenerate stage prefabs via Assets/Racing/Regenerate All Stages.");
                }
            }

            // Pass 2: initialise StageVisuals
            foreach (Stage stage in stages)
            {
                if (stage == null)
                    continue;

                if (!stageVisuals.TryGetValue(stage, out StageVisual sv) || sv == null)
                    continue;

                sv.Initialise(routeLineMaterial);
            }

            // Pass 3: initialise VehicleVisuals
            foreach (Vehicle vehicle in RacePositionTracker.GetAll())
            {
                if (vehicle == null)
                    continue;

                VehicleVisual vv = vehicle.gameObject.GetComponent<VehicleVisual>();
                if (vv == null)
                {
                    Debug.LogWarning($"[TrackVisualizationManager] Vehicle '{vehicle.vehicleName}' is missing a VehicleVisual component. Regenerate vehicle prefabs via Assets/Racing/Regenerate All Vehicles.");
                    continue;
                }

                vv.Initialise(vehicleBaseMaterial);
            }
        }

            }
        }
