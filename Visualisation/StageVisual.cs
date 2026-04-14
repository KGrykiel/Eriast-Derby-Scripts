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

                lr.positionCount = lv.waypoints.Length;
                for (int j = 0; j < lv.waypoints.Length; j++)
                    lr.SetPosition(j, lv.waypoints[j].position);
            }

            RefreshConnectionLines();
        }

        // ==================== PRIVATE ====================

        private void CreateLabel()
        {
            GameObject labelGO = new("Label");
            labelGO.transform.SetParent(transform, false);
            labelGO.transform.position = transform.position + Vector3.up * 2f;
            labelGO.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

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

            foreach (StageLane lane in stage.lanes)
            {
                if (lane == null)
                    continue;

                LaneVisual lv = lane.GetComponent<LaneVisual>();
                if (lv == null || lv.waypoints.Length < 2)
                    continue;

                Gizmos.color = GetLaneColour(lane);
                for (int j = 1; j < lv.waypoints.Length; j++)
                    Gizmos.DrawLine(lv.waypoints[j - 1].position, lv.waypoints[j].position);
            }

            TrackDefinition[] defs = Resources.FindObjectsOfTypeAll<TrackDefinition>();
            if (defs.Length == 0)
                return;

            Stage[] sceneStages = Object.FindObjectsOfType<Stage>();
            Gizmos.color = ConnectionColour;

            foreach (LaneLink link in defs[0].transitions)
            {
                if (link.fromLane == null || link.toLane == null)
                    continue;

                Stage fromPrefabStage = link.fromLane.GetComponentInParent<Stage>(true);
                if (fromPrefabStage == null || fromPrefabStage.stageName != stage.stageName)
                    continue;

                StageLane fromLane = stage.lanes.Find(l => l != null && l.laneName == link.fromLane.laneName);
                if (fromLane == null)
                    continue;

                LaneVisual fromLV = fromLane.GetComponent<LaneVisual>();
                if (fromLV == null || fromLV.waypoints.Length < 2)
                    continue;

                Stage toPrefabStage = link.toLane.GetComponentInParent<Stage>(true);
                if (toPrefabStage == null)
                    continue;

                Stage toStage = null;
                foreach (Stage s in sceneStages)
                {
                    if (s != null && s.stageName == toPrefabStage.stageName)
                    {
                        toStage = s;
                        break;
                    }
                }

                if (toStage == null)
                    continue;

                StageLane toLane = toStage.lanes.Find(l => l != null && l.laneName == link.toLane.laneName);
                if (toLane == null)
                    continue;

                LaneVisual toLV = toLane.GetComponent<LaneVisual>();
                if (toLV == null || toLV.waypoints.Length < 2)
                    continue;

                Gizmos.DrawLine(fromLV.GetPathPosition(1f), toLV.GetPathPosition(0f));
            }
        }
    }
}
