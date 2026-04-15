using UnityEngine;
using UnityEngine.InputSystem;
using Assets.Scripts.Entities.Vehicles;
using Assets.Scripts.Stages;

namespace Assets.Scripts.Visualisation
{
    /// <summary>
    /// RTS-style camera. Drives its own transform directly each frame.
    /// Attach this component to the scene Camera.
    ///
    /// Controls:
    ///   WASD / arrow keys — pan pivot laterally
    ///   MMB drag          — pan pivot laterally
    ///   RMB drag          — orbit (azimuth + elevation)
    ///   Scroll wheel      — zoom in / out
    /// </summary>
    public class CameraController3D : MonoBehaviour
    {
        [Header("Speed")]
        [Tooltip("World units per second when panning with WASD.")]
        [SerializeField] private float panSpeed          = 20f;

        [Tooltip("Degrees of orbit per mouse pixel when dragging with RMB.")]
        [SerializeField] private float orbitSensitivity  = 0.2f;

        [Tooltip("Pan scale per mouse pixel when dragging with MMB. Scales with current zoom distance.")]
        [SerializeField] private float panSensitivity    = 0.05f;

        [Tooltip("World units per scroll wheel tick (normalised to 120 units per detent).")]
        [SerializeField] private float zoomSpeed         = 10f;

        [Tooltip("Lerp speed toward target values each frame.")]
        [SerializeField] private float lerpSpeed         = 8f;

        [Header("Constraints")]
        [SerializeField] private float minDistance  = 5f;
        [SerializeField] private float maxDistance  = 150f;
        [SerializeField] private float minElevation = 20f;
        [SerializeField] private float maxElevation = 80f;

        [Header("Defaults")]
        [SerializeField] private float defaultElevation = 50f;
        [SerializeField] private float defaultDistance  = 40f;

        // ==================== STATE ====================

        // Target values (what we lerp toward)
        private Vector3 _pivotTarget;
        private float   _azimuthTarget;
        private float   _elevationTarget;
        private float   _distanceTarget;

        // Current rendered values
        private Vector3 _pivotCurrent;
        private float   _azimuthCurrent;
        private float   _elevationCurrent;
        private float   _distanceCurrent;

        // ==================== UNITY LIFECYCLE ====================

        private void Start()
        {
            _elevationTarget = _elevationCurrent = defaultElevation;
            _distanceTarget  = _distanceCurrent  = defaultDistance;
        }

        private void Update()
        {
            HandleInput();
            LerpToTargets();
            ApplyCameraTransform();
        }

        // ==================== PUBLIC API ====================

        /// <summary>Smoothly moves the camera pivot to the vehicle's current world position.</summary>
        public void FocusOn(Vehicle vehicle)
        {
            if (vehicle == null)
                return;

            _pivotTarget = vehicle.transform.position;
        }

        /// <summary>Smoothly moves the camera pivot to the stage's world position.</summary>
        public void FocusOn(Stage stage)
        {
            if (stage == null)
                return;

            _pivotTarget = stage.transform.position;
        }

        /// <summary>
        /// Zooms out to a distance that fits all registered stages in view and centres the pivot
        /// on the bounding box of all stage positions.
        /// </summary>
        public void FocusAll()
        {
            var stages = TrackDefinition.GetAll();
            if (stages == null || stages.Count == 0)
                return;

            float minX = float.MaxValue;
            float maxX = float.MinValue;
            float minZ = float.MaxValue;
            float maxZ = float.MinValue;

            foreach (Stage stage in stages)
            {
                if (stage == null)
                    continue;

                Vector3 pos = stage.transform.position;
                if (pos.x < minX) minX = pos.x;
                if (pos.x > maxX) maxX = pos.x;
                if (pos.z < minZ) minZ = pos.z;
                if (pos.z > maxZ) maxZ = pos.z;
            }

            Vector3 centre = new((minX + maxX) / 2f, 0f, (minZ + maxZ) / 2f);
            float extent = Mathf.Max(maxX - minX, maxZ - minZ);
            float requiredDistance = Mathf.Clamp(extent * 1.2f, minDistance, maxDistance);

            _pivotTarget    = centre;
            _distanceTarget = requiredDistance;
        }

        // ==================== INPUT ====================

        private void HandleInput()
        {
            if (Mouse.current == null)
                return;

            HandleRMBOrbit();
            HandleMMBPan();
            HandleScrollZoom();
            HandleWASDPan();
        }

        private void HandleRMBOrbit()
        {
            if (!Mouse.current.rightButton.isPressed)
                return;

            Vector2 delta = Mouse.current.delta.ReadValue();
            _azimuthTarget   += delta.x * orbitSensitivity;
            _elevationTarget -= delta.y * orbitSensitivity;
            _elevationTarget  = Mathf.Clamp(_elevationTarget, minElevation, maxElevation);
        }

        private void HandleMMBPan()
        {
            if (!Mouse.current.middleButton.isPressed)
                return;

            Vector2 delta = Mouse.current.delta.ReadValue();
            Vector3 right   = Quaternion.Euler(0f, _azimuthCurrent, 0f) * Vector3.right;
            Vector3 forward = Quaternion.Euler(0f, _azimuthCurrent, 0f) * Vector3.forward;
            float scale = panSensitivity * (_distanceCurrent / maxDistance);

            _pivotTarget -= delta.x * scale * right;
            _pivotTarget -= delta.y * scale * forward;
        }

        private void HandleScrollZoom()
        {
            // Normalise to detents: most mice report 120 units per physical tick
            float scroll = Mouse.current.scroll.ReadValue().y / 120f;
            if (scroll == 0f)
                return;

            _distanceTarget -= scroll * zoomSpeed;
            _distanceTarget  = Mathf.Clamp(_distanceTarget, minDistance, maxDistance);
        }

        private void HandleWASDPan()
        {
            if (Keyboard.current == null)
                return;

            float h = 0f;
            float v = 0f;

            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)  h -= 1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) h += 1f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed)  v -= 1f;
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed)    v += 1f;

            if (h == 0f && v == 0f)
                return;

            Vector3 right   = Quaternion.Euler(0f, _azimuthCurrent, 0f) * Vector3.right;
            Vector3 forward = Quaternion.Euler(0f, _azimuthCurrent, 0f) * Vector3.forward;

            _pivotTarget += panSpeed * Time.deltaTime * (right * h + forward * v);
        }

        // ==================== LERP + APPLY ====================

        private void LerpToTargets()
        {
            float t = lerpSpeed * Time.deltaTime;
            _pivotCurrent     = Vector3.Lerp(_pivotCurrent, _pivotTarget, t);
            _azimuthCurrent   = Mathf.LerpAngle(_azimuthCurrent, _azimuthTarget, t);
            _elevationCurrent = Mathf.Lerp(_elevationCurrent, _elevationTarget, t);
            _distanceCurrent  = Mathf.Lerp(_distanceCurrent, _distanceTarget, t);
        }

        private void ApplyCameraTransform()
        {
            Vector3 offset = Quaternion.Euler(_elevationCurrent, _azimuthCurrent, 0f)
                             * new Vector3(0f, 0f, -_distanceCurrent);

            transform.position = _pivotCurrent + offset;
            transform.LookAt(_pivotCurrent);
        }
    }
}
