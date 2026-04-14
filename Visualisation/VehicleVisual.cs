using TMPro;
using UnityEngine;
using Assets.Scripts.Entities.Vehicles;
using Assets.Scripts.Entities.Vehicles.VehicleComponents.ComponentTypes;
using Assets.Scripts.Managers;
using Assets.Scripts.Stages;
using Assets.Scripts.Stages.Lanes;

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

        private static readonly Color PlayerColour   = new(0.2f, 1f,   0.2f, 1f);
        private static readonly Color NeutralColour  = new(0.6f, 0.6f, 0.6f, 1f);
        private static readonly Color TerminalColour = new(0.5f, 0.5f, 0.5f, 0.5f);
        private static readonly Color DangerEmissive = new(0.8f, 0f,   0f,   1f);

        private const float SmoothTime            = 0.1f;
        private const float FinishSnapThresholdSq = 0.25f;
        private const float DangerThreshold = 0.3f;
        private const float PulseSpeed      = 3f;

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

        // State
        private bool      _isInTerminalState;
        private bool      _pendingFinish;
        private StageLane _lastLane;
        private Vector3   _velocity;

        // ==================== UNITY LIFECYCLE ====================

        private void Awake()
        {
            _vehicle = GetComponent<Vehicle>();
        }

        private void OnDestroy()
        {
            TurnEventBus.OnVehicleDestroyed -= HandleVehicleDestroyed;
            TurnEventBus.OnVehicleFinished  -= HandleVehicleFinished;

            if (_material != null)          Destroy(_material);
            if (_hpBarBgMaterial != null)   Destroy(_hpBarBgMaterial);
            if (_hpBarFillMaterial != null) Destroy(_hpBarFillMaterial);
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

            TurnEventBus.OnVehicleDestroyed += HandleVehicleDestroyed;
            TurnEventBus.OnVehicleFinished  += HandleVehicleFinished;
        }

        // ==================== POSITION + FACING ====================

        private void UpdatePosition()
        {
            Stage stage     = RacePositionTracker.GetStage(_vehicle);
            StageLane lane  = RacePositionTracker.GetLane(_vehicle);
            if (stage == null || lane == null)
                return;

            LaneVisual lv = lane.GetComponent<LaneVisual>();
            if (lv == null)
                return;

            float t = Mathf.Clamp01((float)RacePositionTracker.GetProgress(_vehicle) / stage.length);
            Vector3 target = _pendingFinish ? lv.GetPathPosition(1f) : lv.GetPathPosition(t);

            bool laneChanged = lane != _lastLane;
            _lastLane = lane;

            if (laneChanged)
            {
                // Snap to the entry of the new lane; carry velocity magnitude into the new lane direction
                transform.position = lv.GetPathPosition(0f);
                float speed = _velocity.magnitude;
                _velocity = lv.GetPathTangent(0f) * speed;
            }
            else
            {
                transform.position = Vector3.SmoothDamp(transform.position, target, ref _velocity, SmoothTime);
            }

            // Face the direction of travel while moving; fall back to path tangent at rest
            Vector3 facing = _velocity.sqrMagnitude > 0.01f ? _velocity.normalized : lv.GetPathTangent(t);
            if (facing != Vector3.zero)
                transform.forward = facing;

            // Freeze once the smooth arrival at the finish is complete
            if (_pendingFinish && (transform.position - target).sqrMagnitude < FinishSnapThresholdSq)
            {
                transform.position = target;
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

        private void HandleVehicleDestroyed(Vehicle vehicle)
        {
            if (vehicle != _vehicle)
                return;

            SetTerminalState();
        }

        private void HandleVehicleFinished(Vehicle vehicle)
        {
            if (vehicle != _vehicle)
                return;

            _pendingFinish = true;
        }

        private void SetTerminalState()
        {
            _isInTerminalState = true;
            StopAllCoroutines();

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
                _material = new Material(source);
                _material.color = GetVehicleColour();
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
