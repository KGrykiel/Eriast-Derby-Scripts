using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Assets.Scripts.Stages;
using Assets.Scripts.Stages.Lanes;

namespace Assets.Scripts.Visualisation
{
    /// <summary>
    /// Visual-only component on each Stage GameObject.
    /// Manages the world-space stage name label and per-lane route LineRenderers.
    /// </summary>
    public class StageVisual : MonoBehaviour
    {
        [Header("Route Lines")]
        [Tooltip("Number of sample points used to draw each lane's route line.")]
        public int routeSampleCount = 20;

        [Tooltip("Width of each route line in world units.")]
        public float lineWidth = 0.1f;

        [Header("Connection Lines")]
        [Tooltip("Width of the inter-stage connection indicator lines.")]
        public float connectionLineWidth = 0.04f;

        private static readonly Color HazardousColour = new(0.9f, 0.15f, 0.15f, 1f);
        private static readonly Color DefaultColour = Color.white;
        private static readonly Color ConnectionColour = new(0.55f, 0.55f, 0.55f, 0.6f);

        private Stage _stage;
        private readonly List<LineRenderer> _routeLines = new();
        private readonly List<LineRenderer> _connectionLines = new();
        private bool _isInitialised;

        private void Awake()
        {
            _stage = GetComponent<Stage>();
        }

        // ==================== PUBLIC API ====================

        /// <summary>
        /// Creates the stage name label and per-lane route LineRenderers, then draws the initial lines.
        /// Safe to call multiple times — subsequent calls only redraw.
        /// </summary>
        public void Initialise(Material lineMaterial)
        {
            if (_isInitialised)
            {
                Refresh();
                return;
            }

            _isInitialised = true;
            CreateLabel();
            CreateRouteLines(lineMaterial);
            CreateConnectionLines(lineMaterial);
            CreateClickCollider();
            Refresh();
        }

        /// <summary>
        /// Redraws all route LineRenderers by resampling each lane's LaneVisual path.
        /// Call this after updating LaneVisual waypoints (e.g., from the Blender sync tool).
        /// </summary>
        public void Refresh()
        {
            if (_stage == null)
                return;

            for (int i = 0; i < _stage.lanes.Count && i < _routeLines.Count; i++)
            {
                StageLane lane = _stage.lanes[i];
                LineRenderer lr = _routeLines[i];
                if (lane == null || lr == null)
                    continue;

                LaneVisual lv = lane.GetComponent<LaneVisual>();
                if (lv == null)
                    continue;

                lr.positionCount = routeSampleCount;
                for (int j = 0; j < routeSampleCount; j++)
                {
                    float t = j / (routeSampleCount - 1f);
                    lr.SetPosition(j, lv.GetPathPosition(t));
                }
            }

            RefreshConnectionLines();
        }

        // ==================== PRIVATE ====================

        private void CreateLabel()
        {
            GameObject labelGO = new("Label");
            labelGO.transform.SetParent(transform, false);
            labelGO.transform.localPosition = new Vector3(0f, 2f, 0f);
            labelGO.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);

            TextMeshPro label = labelGO.AddComponent<TextMeshPro>();
            label.text = _stage != null ? _stage.stageName : name;
            label.alignment = TextAlignmentOptions.Center;
            label.fontSize = 3f;
            label.color = Color.white;
        }

        private void CreateRouteLines(Material lineMaterial)
        {
            if (_stage == null)
                return;

            _routeLines.Clear();

            for (int i = 0; i < _stage.lanes.Count; i++)
            {
                StageLane lane = _stage.lanes[i];
                Color laneColour = lane != null ? GetLaneColour(lane) : DefaultColour;

                GameObject lineGO = new($"RouteLine_{i}");
                lineGO.transform.SetParent(transform, false);

                LineRenderer lr = lineGO.AddComponent<LineRenderer>();
                lr.useWorldSpace = true;
                lr.startWidth = lineWidth;
                lr.endWidth = lineWidth;
                lr.startColor = laneColour;
                lr.endColor = laneColour;
                lr.positionCount = 0;

                if (lineMaterial != null)
                    lr.material = lineMaterial;

                _routeLines.Add(lr);
            }
        }

        private void CreateConnectionLines(Material lineMaterial)
        {
            if (_stage == null)
                return;

            _connectionLines.Clear();

            for (int i = 0; i < _stage.lanes.Count; i++)
            {
                GameObject lineGO = new($"ConnectionLine_{i}");
                lineGO.transform.SetParent(transform, false);

                LineRenderer lr = lineGO.AddComponent<LineRenderer>();
                lr.useWorldSpace = true;
                lr.startWidth = connectionLineWidth;
                lr.endWidth = connectionLineWidth;
                lr.startColor = ConnectionColour;
                lr.endColor = ConnectionColour;
                lr.positionCount = 0;

                if (lineMaterial != null)
                    lr.material = lineMaterial;

                _connectionLines.Add(lr);
            }
        }

        private void RefreshConnectionLines()
        {
            if (TrackDefinition.Active == null)
                return;

            for (int i = 0; i < _stage.lanes.Count && i < _connectionLines.Count; i++)
            {
                StageLane lane = _stage.lanes[i];
                LineRenderer lr = _connectionLines[i];
                if (lane == null || lr == null)
                    continue;

                LaneVisual lv = lane.GetComponent<LaneVisual>();
                if (lv == null || lv.waypoints.Length == 0)
                {
                    lr.positionCount = 0;
                    continue;
                }

                StageLane targetLane = TrackDefinition.Active.GetTargetLane(lane);
                if (targetLane == null)
                {
                    lr.positionCount = 0;
                    continue;
                }

                LaneVisual targetLV = targetLane.GetComponent<LaneVisual>();
                if (targetLV == null || targetLV.waypoints.Length == 0)
                {
                    lr.positionCount = 0;
                    continue;
                }

                lr.positionCount = 2;
                lr.SetPosition(0, lv.GetPathPosition(1f));
                lr.SetPosition(1, targetLV.GetPathPosition(0f));
            }
        }

        private void OnMouseDown()
        {
            TrackVisualizationManager.RaiseStageClicked(_stage);
        }

        private void CreateClickCollider()
        {
            BoxCollider col = gameObject.GetComponent<BoxCollider>();
            if (col == null)
                col = gameObject.AddComponent<BoxCollider>();

            col.center = new Vector3(0f, 0.5f, 0f);
            col.size   = new Vector3(3f, 1f, 3f);
        }

        private static Color GetLaneColour(StageLane lane)
        {
            bool isHazardous = lane.turnEffects != null && lane.turnEffects.Count > 0;
            return isHazardous ? HazardousColour : DefaultColour;
        }

        // ==================== GIZMOS ====================

        private void OnDrawGizmos()
        {
            if (Application.isPlaying)
                return;

            Stage stage = GetComponent<Stage>();
            if (stage == null)
                return;

            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(transform.position, 0.5f);
        }
    }
}
