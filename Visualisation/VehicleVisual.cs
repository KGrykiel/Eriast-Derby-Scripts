using System.Collections;
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

        private static readonly Color PlayerColour   = new Color(0.2f, 1f,   0.2f, 1f);
        private static readonly Color NeutralColour  = new Color(0.6f, 0.6f, 0.6f, 1f);
        private static readonly Color TerminalColour = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        private static readonly Color DangerEmissive = new Color(0.8f, 0f,   0f,   1f);
        private static readonly Color BadgeColour    = new Color(1f,   0.85f, 0.3f, 1f);

        private static readonly Vector3 MeshBaseScale     = new Vector3(0.8f, 0.4f, 1.4f);
        private static readonly Vector3 MeshTerminalScale = new Vector3(0.7f, 0.3f, 1.1f);

        private const float SmoothTime             = 0.1f;
        private const float FinishSnapThresholdSq  = 0.25f;
        private const float DangerThreshold = 0.3f;
        private const float PulseSpeed      = 3f;
        private const float HpBarWidth      = 1.5f;
        private const float HpBarHeight     = 0.12f;
        private const float OverlayBarY     = 0.65f;
        private const float OverlayBarFillZ = -0.01f;

        // ==================== PRIVATE FIELDS ====================

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
            TurnEventBus.OnTurnEnded        -= HandleTurnEnded;
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
            CreateOverlayRoot();
            CreateLabel();
            CreateHpBar();
            CreateConditionBadge();

            TurnEventBus.OnTurnEnded        += HandleTurnEnded;
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
                (hpPercent - 1f) * HpBarWidth / 2f, OverlayBarY, OverlayBarFillZ);

            _hpBarFill.transform.localScale = new Vector3(hpPercent * HpBarWidth, HpBarHeight * 0.8f, 1f);

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

        private void HandleTurnEnded(Vehicle vehicle)
        {
            if (vehicle != _vehicle)
                return;

            StartCoroutine(PulseScaleCoroutine());
        }

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

            if (_meshChild != null)
                _meshChild.transform.localScale = MeshTerminalScale;
        }

        // ==================== PULSE ANIMATION ====================

        private IEnumerator PulseScaleCoroutine()
        {
            if (_meshChild == null || _isInTerminalState)
                yield break;

            const float duration = 0.15f;
            Vector3 expandedScale = MeshBaseScale * 1.4f;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                if (_isInTerminalState || _meshChild == null) yield break;
                elapsed += Time.deltaTime;
                _meshChild.transform.localScale = Vector3.Lerp(MeshBaseScale, expandedScale, elapsed / duration);
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < duration)
            {
                if (_isInTerminalState || _meshChild == null) yield break;
                elapsed += Time.deltaTime;
                _meshChild.transform.localScale = Vector3.Lerp(expandedScale, MeshBaseScale, elapsed / duration);
                yield return null;
            }

            _meshChild.transform.localScale = MeshBaseScale;
        }

        // ==================== SETUP ====================

        private void CreateMeshChild(Material baseMaterial)
        {
            _meshChild = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _meshChild.transform.SetParent(transform, false);
            _meshChild.transform.localScale = MeshBaseScale;
            _meshChild.name = "Mesh";

            MeshRenderer meshRenderer = _meshChild.GetComponent<MeshRenderer>();
            _material = baseMaterial != null ? new Material(baseMaterial) : meshRenderer.material;
            _material.color = GetVehicleColour();
            meshRenderer.material = _material;

            // Click collider is Phase 4 — remove the one CreatePrimitive adds
            Collider col = _meshChild.GetComponent<Collider>();
            if (col != null)
                Destroy(col);
        }

        private void CreateClickCollider()
        {
            BoxCollider col = gameObject.GetComponent<BoxCollider>();
            if (col == null)
                col = gameObject.AddComponent<BoxCollider>();

            col.center = new Vector3(0f, 0.2f, 0f);
            col.size   = MeshBaseScale;
        }

        private void CreateOverlayRoot()
        {
            _overlayRoot = new GameObject("Overlay");
            _overlayRoot.transform.SetParent(transform, false);
            _overlayRoot.transform.localPosition = Vector3.zero;
        }

        private void CreateLabel()
        {
            GameObject labelGO = new GameObject("Label");
            labelGO.transform.SetParent(_overlayRoot.transform, false);
            labelGO.transform.localPosition = new Vector3(0f, 1f, 0f);

            _label = labelGO.AddComponent<TextMeshPro>();
            _label.text = _vehicle != null ? _vehicle.vehicleName : name;
            _label.alignment = TextAlignmentOptions.Center;
            _label.fontSize = 2f;
            _label.color = Color.white;
        }

        private void CreateHpBar()
        {
            // Background
            GameObject bgGO = GameObject.CreatePrimitive(PrimitiveType.Quad);
            bgGO.name = "HpBarBg";
            bgGO.transform.SetParent(_overlayRoot.transform, false);
            bgGO.transform.localPosition = new Vector3(0f, OverlayBarY, 0f);
            bgGO.transform.localScale = new Vector3(HpBarWidth, HpBarHeight, 1f);

            _hpBarBgMaterial = bgGO.GetComponent<MeshRenderer>().material;
            _hpBarBgMaterial.color = new Color(0.15f, 0.15f, 0.15f, 0.85f);

            Collider bgCol = bgGO.GetComponent<Collider>();
            if (bgCol != null) Destroy(bgCol);

            // Fill
            _hpBarFill = GameObject.CreatePrimitive(PrimitiveType.Quad);
            _hpBarFill.name = "HpBarFill";
            _hpBarFill.transform.SetParent(_overlayRoot.transform, false);
            _hpBarFill.transform.localPosition = new Vector3(0f, OverlayBarY, OverlayBarFillZ);
            _hpBarFill.transform.localScale = new Vector3(HpBarWidth, HpBarHeight * 0.8f, 1f);

            _hpBarFillMaterial = _hpBarFill.GetComponent<MeshRenderer>().material;
            _hpBarFillMaterial.color = GetHpColour(1f);

            Collider fillCol = _hpBarFill.GetComponent<Collider>();
            if (fillCol != null) Destroy(fillCol);
        }

        private void CreateConditionBadge()
        {
            GameObject badgeGO = new GameObject("ConditionBadge");
            badgeGO.transform.SetParent(_overlayRoot.transform, false);
            badgeGO.transform.localPosition = new Vector3(HpBarWidth / 2f + 0.3f, OverlayBarY, 0f);

            _conditionBadge = badgeGO.AddComponent<TextMeshPro>();
            _conditionBadge.alignment = TextAlignmentOptions.Center;
            _conditionBadge.fontSize = 2f;
            _conditionBadge.color = BadgeColour;

            badgeGO.SetActive(false);
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
