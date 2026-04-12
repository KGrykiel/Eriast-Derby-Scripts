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
        [Header("Layout")]
        [Tooltip("World-space distance between adjacent lane route lines.")]
        public float laneSpacing = 2f;

        [Header("Route Lines")]
        [Tooltip("Number of sample points used to draw each lane's route line.")]
        public int routeSampleCount = 20;

        [Tooltip("Width of each route line in world units.")]
        public float lineWidth = 0.1f;

        private static readonly Color HazardousColour = new(0.9f, 0.15f, 0.15f, 1f);
        private static readonly Color DefaultColour = Color.white;

        private Stage _stage;
        private readonly List<LineRenderer> _routeLines = new();
        private bool _isInitialised;

        private void Awake()
        {
            _stage = GetComponent<Stage>();
        }

        // ==================== PUBLIC API ====================

        /// <summary>
        /// Returns the world-space position for lane index <paramref name="laneIndex"/> in this stage,
        /// distributed perpendicularly to the stage's travel direction.
        /// </summary>
        public Vector3 ComputeLanePosition(int laneIndex)
        {
            if (_stage == null)
                _stage = GetComponent<Stage>();

            int laneCount = _stage != null ? _stage.lanes.Count : 1;
            Vector3 travelDir = GetTravelDirection(_stage);
            Vector3 perpendicular = Vector3.Cross(travelDir, Vector3.up).normalized;
            float offset = (laneIndex - (laneCount - 1) / 2f) * laneSpacing;
            return transform.position + perpendicular * offset;
        }

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

        private static Vector3 GetTravelDirection(Stage stage)
        {
            if (stage == null)
                return Vector3.forward;

            var connected = TrackDefinition.GetConnected(stage);
            Vector3 sum = Vector3.zero;
            int count = 0;

            foreach (Stage next in connected)
            {
                if (next == null)
                    continue;
                sum += (next.transform.position - stage.transform.position).normalized;
                count++;
            }

            if (count == 0)
                return Vector3.forward;

            return sum.normalized;
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

            if (stage.lanes == null)
                return;

            for (int i = 0; i < stage.lanes.Count; i++)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawSphere(ComputeLanePosition(i), 0.25f);
            }
        }
    }
}
