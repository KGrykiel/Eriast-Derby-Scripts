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

        private const int   DashCount    = 8;
        private const float DashFraction = 0.4f;

        [Header("Label")]
        [SerializeField] private GameObject _stageLabelPrefab;

        private Stage _stage;
        private TextMeshPro _label;
        private readonly List<LineRenderer>  _routeLines          = new();
        private readonly List<LineRenderer>  _connectionLines     = new();
        private readonly List<Material>      _routeLineMaterials  = new();
        private Material                     _connectionDashMaterial;
        private Texture2D                    _dashTexture;
        private bool _isInitialised;

        private void Awake()
        {
            _stage = GetComponent<Stage>();
        }

        private void OnDestroy()
        {
            foreach (Material mat in _routeLineMaterials)
                if (mat != null) Destroy(mat);

            if (_connectionDashMaterial != null)
                Destroy(_connectionDashMaterial);

            if (_dashTexture != null)
                Destroy(_dashTexture);
        }

        private void LateUpdate()
        {
            UpdateLabelBillboard();
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

        private void UpdateLabelBillboard()
        {
            if (_label == null)
                return;

            Camera cam = Camera.main;
            if (cam != null)
                _label.transform.rotation = cam.transform.rotation;
        }

        private void CreateLabel()
        {
            if (_stageLabelPrefab == null)
            {
                Debug.LogWarning($"[StageVisual] No Stage Label prefab assigned on '{name}'.");
                return;
            }

            GameObject instance = Instantiate(_stageLabelPrefab, transform);
            instance.transform.position = transform.position + Vector3.up * 2f;
            _label = instance.GetComponent<TextMeshPro>();

            if (_label != null)
                _label.text = _stage != null ? _stage.stageName : name;
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
                lineGO.transform.SetParent(lane != null ? lane.transform : transform, false);

                LineRenderer lr = lineGO.AddComponent<LineRenderer>();
                lr.useWorldSpace = true;
                lr.startWidth    = lineWidth;
                lr.endWidth      = lineWidth;
                lr.startColor    = Color.white;
                lr.endColor      = Color.white;
                lr.positionCount = 0;

                Material mat = lineMaterial != null
                    ? new Material(lineMaterial)
                    : new Material(Shader.Find("Sprites/Default"));
                mat.color    = laneColour;
                lr.material  = mat;
                _routeLineMaterials.Add(mat);

                _routeLines.Add(lr);
            }
        }

        private void CreateConnectionLines(Material lineMaterial)
        {
            if (_stage == null)
                return;

            _connectionLines.Clear();

            _dashTexture            = CreateDashTexture();
            _connectionDashMaterial = new Material(Shader.Find("Sprites/Default"))
            {
                color = ConnectionColour,
                mainTexture = _dashTexture
            };

            for (int i = 0; i < _stage.lanes.Count; i++)
            {
                StageLane lane = _stage.lanes[i];

                GameObject lineGO = new($"ConnectionLine_{i}");
                lineGO.transform.SetParent(lane != null ? lane.transform : transform, false);
                lineGO.SetActive(false);

                LineRenderer lr = lineGO.AddComponent<LineRenderer>();
                lr.useWorldSpace = true;
                lr.startWidth    = connectionLineWidth;
                lr.endWidth      = connectionLineWidth;
                lr.startColor    = Color.white;
                lr.endColor      = Color.white;
                lr.positionCount = 2;
                lr.textureMode   = LineTextureMode.DistributePerSegment;
                lr.material      = _connectionDashMaterial;

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
                    lr.gameObject.SetActive(false);
                    continue;
                }

                StageLane targetLane = TrackDefinition.Active.GetTargetLane(lane);
                if (targetLane == null)
                {
                    lr.gameObject.SetActive(false);
                    continue;
                }

                LaneVisual targetLV = targetLane.GetComponent<LaneVisual>();
                if (targetLV == null || targetLV.waypoints.Length == 0)
                {
                    lr.gameObject.SetActive(false);
                    continue;
                }

                lr.gameObject.SetActive(true);
                lr.SetPosition(0, lv.GetPathPosition(1f));
                lr.SetPosition(1, targetLV.GetPathPosition(0f));
            }
        }

        private static Texture2D CreateDashTexture()
        {
            const int pixelsPerUnit = 8;
            int totalPixels        = DashCount * pixelsPerUnit;
            int dashPixels         = Mathf.RoundToInt(pixelsPerUnit * DashFraction);

            Texture2D tex = new Texture2D(totalPixels, 1, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            Color[] pixels = new Color[totalPixels];
            for (int x = 0; x < totalPixels; x++)
            {
                int posInUnit = x % pixelsPerUnit;
                pixels[x] = posInUnit < dashPixels ? Color.white : Color.clear;
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
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

            Stage[] sceneStages = FindObjectsByType<Stage>(FindObjectsSortMode.None);
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
