using System.Collections;
using TMPro;
using UnityEngine;
using Assets.Scripts.Entities.Vehicles;
using Assets.Scripts.Entities.Vehicles.VehicleComponents.ComponentTypes;
using Assets.Scripts.Stages;
using Assets.Scripts.Stages.Lanes;
using Assets.Scripts.Managers.Turn;
using Assets.Scripts.Managers.Race;

namespace Assets.Scripts.Visualisation
{
    [RequireComponent(typeof(Vehicle))]
    /// <summary>
    /// Visual-only component on each Vehicle GameObject.
    /// Handles the cube mesh, colour-coded material, billboard overlays (name label, HP bar,
    /// condition badge), danger emissive pulse, pulse-on-act animation, and terminal state
    /// visuals for destroyed and finished vehicles.
    /// </summary>
    public class VehicleVisual : MonoBehaviour
    {
        // ==================== CONSTANTS ====================

        private static readonly Color PlayerColour     = new(0.2f, 1f,   0.2f, 1f);
        private static readonly Color NeutralColour    = new(0.6f, 0.6f, 0.6f, 1f);
        private static readonly Color TerminalColour   = new(0.5f, 0.5f, 0.5f, 0.5f);
        private static readonly Color DangerEmissive   = new(0.8f, 0f,   0f,   1f);
        private static readonly Color ActingEmissive   = new(1f,   1f,   0.3f, 1f);

        private const float SmoothTime         = 0.1f;
        private const float FinishSnapThreshold = 0.005f;
        private const float DangerThreshold     = 0.3f;
        private const float PulseSpeed          = 3f;

        // ==================== PRIVATE FIELDS ====================

        [SerializeField] private GameObject _vehicleUIPrefab;

        private Vehicle _vehicle;

        // Mesh
        private GameObject _meshChild;
        private Material   _material;

        // Overlay root — single GO whose rotation tracks the camera; all labels/bars are children
        private GameObject _overlayRoot;
        private TextMeshPro _label;

        // HP bar
        private GameObject _hpBarFill;
        private Material   _hpBarBgMaterial;
        private Material   _hpBarFillMaterial;
        private float      _hpBarFullWidth;
        private float      _hpBarFullHeight;
        private float      _hpBarY;
        private float      _hpBarFillZ;

        // Condition badge
        private TextMeshPro _conditionBadge;

        // Action line
        private LineRenderer  _actionLine;
        private Material      _actionLineMaterial;
        private VehicleVisual _actionLineTarget;

        // Action label
        private TextMeshPro _actionLabel;

        // State
        private bool      _isInTerminalState;
        private bool      _isActing;
        private bool      _pendingFinish;
        private StageLane _lastLane;
        private float     _visualT;
        private float     _tVelocity;

        // ==================== UNITY LIFECYCLE ====================

        private void Awake()
        {
            _vehicle = GetComponent<Vehicle>();
        }

        private void OnDestroy()
        {
            TurnEventBus.OnEvent -= HandleTurnEvent;

            if (_material != null)           Destroy(_material);
            if (_hpBarBgMaterial != null)    Destroy(_hpBarBgMaterial);
            if (_hpBarFillMaterial != null)  Destroy(_hpBarFillMaterial);
            if (_actionLineMaterial != null) Destroy(_actionLineMaterial);
        }

        private void LateUpdate()
        {
            if (_vehicle == null || _meshChild == null)
                return;

            if (!_isInTerminalState)
            {
                UpdatePosition();
                UpdateDangerPulse();
            }

            UpdateOverlayBillboard();
            UpdateHpBar();
            UpdateConditionBadge();
            UpdateActionLine();
        }

        // ==================== PUBLIC API ====================

        /// <summary>
        /// Creates the vehicle mesh, billboard overlays, and subscribes to turn events.
        /// Call once from TrackVisualizationManager after all registries are ready.
        /// </summary>
        public void Initialise(Material baseMaterial)
        {
            CreateMeshChild(baseMaterial);
            CreateClickCollider();
            AttachOverlayUI();
            CreateActionLine();

            TurnEventBus.OnEvent += HandleTurnEvent;
        }

        /// <summary>World-space centre of the vehicle mesh. Use this as a line endpoint instead of transform.position.</summary>
        public Vector3 MeshCentre
        {
            get
            {
                if (_meshChild == null)
                    return transform.position;

                Renderer r = _meshChild.GetComponent<Renderer>();
                return r != null ? r.bounds.center : transform.position;
            }
        }

        /// <summary>
        /// Draws a line from this vehicle to the target and keeps it updated until hidden.
        /// </summary>
        public void ShowActionLine(VehicleVisual target)
        {
            if (_actionLine == null || target == null)
                return;

            _actionLineTarget   = target;
            _actionLine.enabled = true;
        }

        /// <summary>Removes the action line drawn by ShowActionLine.</summary>
        public void HideActionLine()
        {
            if (_actionLine != null)
                _actionLine.enabled = false;

            _actionLineTarget = null;
        }

        /// <summary>Shows a floating label above this vehicle with the given skill name.</summary>
        public void ShowActionLabel(string skillName)
        {
            if (_actionLabel == null)
                return;

            _actionLabel.text = skillName;
            _actionLabel.gameObject.SetActive(true);
        }

        /// <summary>Hides the floating skill label shown by ShowActionLabel.</summary>
        public void HideActionLabel()
        {
            if (_actionLabel != null)
                _actionLabel.gameObject.SetActive(false);
        }

        /// <summary>Applies a steady emissive highlight to indicate this vehicle is currently acting.</summary>
        public void ShowActingHighlight()
        {
            _isActing = true;
        }

        /// <summary>Removes the acting highlight applied by ShowActingHighlight.</summary>
        public void HideActingHighlight()
        {
            _isActing = false;
        }

        // ==================== POSITION + FACING ====================

        private void UpdatePosition()
        {
            Stage stage    = RacePositionTracker.GetStage(_vehicle);
            StageLane lane = RacePositionTracker.GetLane(_vehicle);
            if (stage == null || lane == null)
                return;

            LaneVisual lv = lane.GetComponent<LaneVisual>();
            if (lv == null)
                return;

            float logicalT = Mathf.Clamp01((float)RacePositionTracker.GetProgress(_vehicle) / stage.length);
            float targetT  = _pendingFinish ? 1f : logicalT;

            bool laneChanged = lane != _lastLane;
            _lastLane = lane;

            if (laneChanged)
            {
                // Snap to the entry of the new lane
                _visualT   = 0f;
                _tVelocity = 0f;
            }
            else
            {
                _visualT = Mathf.SmoothDamp(_visualT, targetT, ref _tVelocity, SmoothTime);
            }

            transform.position = lv.GetPathPosition(_visualT);

            // Face the direction of travel along the path
            Vector3 facing = lv.GetPathTangent(_visualT);
            if (facing != Vector3.zero)
                transform.forward = facing;

            // Freeze once the visual has reached the end of the finish lane
            if (_pendingFinish && _visualT >= 1f - FinishSnapThreshold)
            {
                _visualT           = 1f;
                transform.position = lv.GetPathPosition(1f);
                SetTerminalState();
            }
        }

        // ==================== OVERLAY ====================

        private void UpdateOverlayBillboard()
        {
            if (_overlayRoot == null)
                return;

            Camera cam = Camera.main;
            if (cam != null)
                _overlayRoot.transform.rotation = cam.transform.rotation;
        }

        private void UpdateHpBar()
        {
            if (_hpBarFill == null || _vehicle == null)
                return;

            ChassisComponent chassis = _vehicle.Chassis;
            if (chassis == null)
                return;

            int maxHp = chassis.GetBaseMaxHealth();
            float hpPercent = maxHp > 0 ? Mathf.Clamp01((float)chassis.GetCurrentHealth() / maxHp) : 0f;

            // Left-align the fill: offset X so the left edge stays fixed as the bar shrinks
            _hpBarFill.transform.localPosition = new Vector3(
                (hpPercent - 1f) * _hpBarFullWidth / 2f, _hpBarY, _hpBarFillZ);

            _hpBarFill.transform.localScale = new Vector3(hpPercent * _hpBarFullWidth, _hpBarFullHeight, 1f);

            _hpBarFillMaterial.color = GetHpColour(hpPercent);
        }

        private void UpdateConditionBadge()
        {
            if (_conditionBadge == null || _vehicle == null)
                return;

            int count = _vehicle.GetActiveVehicleConditions().Count;
            bool hasConditions = count > 0;

            _conditionBadge.gameObject.SetActive(hasConditions);
            if (hasConditions)
                _conditionBadge.text = count.ToString();
        }

        // ==================== DANGER PULSE ====================

        private void UpdateDangerPulse()
        {
            if (_material == null || _vehicle == null)
                return;

            // Acting highlight takes priority over the danger pulse.
            if (_isActing)
            {
                _material.EnableKeyword("_EMISSION");
                _material.SetColor("_EmissionColor", ActingEmissive);
                return;
            }

            ChassisComponent chassis = _vehicle.Chassis;
            if (chassis == null)
                return;

            int maxHp = chassis.GetBaseMaxHealth();
            float hpPercent = maxHp > 0 ? (float)chassis.GetCurrentHealth() / maxHp : 0f;
            bool isInDanger = hpPercent > 0f && hpPercent < DangerThreshold;

            if (isInDanger)
            {
                float pulse = Mathf.Sin(Time.time * PulseSpeed) * 0.5f + 0.5f;
                _material.EnableKeyword("_EMISSION");
                _material.SetColor("_EmissionColor", DangerEmissive * pulse);
            }
            else
            {
                _material.DisableKeyword("_EMISSION");
                _material.SetColor("_EmissionColor", Color.black);
            }
        }

        // ==================== EVENT HANDLERS ====================

        private void HandleTurnEvent(TurnEvent evt)
        {
            if (evt is VehicleDestroyedEvent d && d.Vehicle == _vehicle) SetTerminalState();
            else if (evt is VehicleFinishedEvent f && f.Vehicle == _vehicle) _pendingFinish = true;
        }

        private void SetTerminalState()
        {
            _isInTerminalState = true;
            StopAllCoroutines();
            HideActionLine();
            HideActionLabel();

            if (_material != null)
            {
                _material.color = TerminalColour;
                _material.DisableKeyword("_EMISSION");
                _material.SetColor("_EmissionColor", Color.black);
            }
        }

        // ==================== SETUP ====================

        private void CreateMeshChild(Material baseMaterial)
        {
            Transform existingMesh = transform.Find("Mesh");
            if (existingMesh != null)
            {
                _meshChild = existingMesh.gameObject;
            }
            else
            {
                _meshChild = GameObject.CreatePrimitive(PrimitiveType.Cube);
                _meshChild.transform.SetParent(transform, false);
                _meshChild.name = "Mesh";

                // Click collider is Phase 4 — remove the one CreatePrimitive adds
                Collider col = _meshChild.GetComponent<Collider>();
                if (col != null)
                    Destroy(col);
            }

            MeshRenderer[] renderers = _meshChild.GetComponentsInChildren<MeshRenderer>(true);
            if (renderers.Length > 0)
            {
                Material source = baseMaterial != null ? baseMaterial : renderers[0].sharedMaterial;
                _material = new Material(source)
                {
                    color = GetVehicleColour()
                };
                foreach (MeshRenderer mr in renderers)
                    mr.material = _material;
            }
        }

        private void CreateClickCollider()
        {
            BoxCollider col = gameObject.GetComponent<BoxCollider>();
            if (col == null)
                col = gameObject.AddComponent<BoxCollider>();

            col.center = new Vector3(0f, 0.2f, 0f);
            col.size   = Vector3.one;
        }

        private void AttachOverlayUI()
        {
            if (_vehicleUIPrefab == null)
            {
                Debug.LogWarning($"[VehicleVisual] No Vehicle UI prefab assigned on '{name}'.");
                return;
            }

            GameObject instance = Instantiate(_vehicleUIPrefab, transform);
            instance.transform.localPosition = Vector3.zero;
            _overlayRoot = instance;

            Transform labelT = instance.transform.Find("Label");
            if (labelT != null)
            {
                _label = labelT.GetComponent<TextMeshPro>();
                if (_label != null)
                    _label.text = _vehicle != null ? _vehicle.vehicleName : name;
            }

            Transform bgT = instance.transform.Find("HpBarBg");
            if (bgT != null)
                _hpBarBgMaterial = bgT.GetComponent<MeshRenderer>().material;

            Transform fillT = instance.transform.Find("HpBarFill");
            if (fillT != null)
            {
                _hpBarFill         = fillT.gameObject;
                _hpBarFillMaterial = fillT.GetComponent<MeshRenderer>().material;
                _hpBarFullWidth    = _hpBarFill.transform.localScale.x;
                _hpBarFullHeight   = _hpBarFill.transform.localScale.y;
                _hpBarY            = _hpBarFill.transform.localPosition.y;
                _hpBarFillZ        = _hpBarFill.transform.localPosition.z;
            }

            Transform badgeT = instance.transform.Find("ConditionBadge");
            if (badgeT != null)
                _conditionBadge = badgeT.GetComponent<TextMeshPro>();

            GameObject actionLabelGO = new GameObject("ActionLabel");
            actionLabelGO.transform.SetParent(instance.transform, false);
            actionLabelGO.transform.localPosition = new Vector3(0f, 0.8f, 0f);
            _actionLabel            = actionLabelGO.AddComponent<TextMeshPro>();
            _actionLabel.alignment  = TextAlignmentOptions.Center;
            _actionLabel.fontSize   = 3f;
            _actionLabel.fontStyle  = FontStyles.Bold;
            _actionLabel.color      = new Color(1f, 0.9f, 0f, 1f);
            _actionLabel.outlineWidth = 0.2f;
            _actionLabel.outlineColor = new Color32(0, 0, 0, 255);
            _actionLabel.gameObject.SetActive(false);
        }

        private void CreateActionLine()
        {
            _actionLine = gameObject.AddComponent<LineRenderer>();
            _actionLineMaterial         = new Material(Shader.Find("Sprites/Default"));
            _actionLine.material        = _actionLineMaterial;
            _actionLine.useWorldSpace   = true;
            _actionLine.positionCount   = 2;
            _actionLine.startWidth      = 0.12f;
            _actionLine.endWidth        = 0.02f;
            _actionLine.startColor      = new Color(1f, 0.55f, 0f, 1f);
            _actionLine.endColor        = new Color(1f, 0.55f, 0f, 1f);
            _actionLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _actionLine.receiveShadows  = false;
            _actionLine.enabled         = false;
        }

        private void UpdateActionLine()
        {
            if (_actionLine == null || !_actionLine.enabled || _actionLineTarget == null)
                return;

            _actionLine.SetPosition(0, MeshCentre);
            _actionLine.SetPosition(1, _actionLineTarget.MeshCentre);
        }

        // ==================== HELPERS ====================

        private Color GetVehicleColour()
        {
            if (_vehicle == null)
                return NeutralColour;

            if (_vehicle.controlType == ControlType.Player)
                return PlayerColour;

            if (_vehicle.team != null)
                return _vehicle.team.teamColour;

            return NeutralColour;
        }

        private static Color GetHpColour(float hpPercent)
        {
            if (hpPercent > 0.6f) return Color.green;
            if (hpPercent > 0.3f) return Color.yellow;
            return Color.red;
        }
    }
}
