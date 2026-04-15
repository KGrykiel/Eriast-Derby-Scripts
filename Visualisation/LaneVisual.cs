using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Visualisation
{
    /// <summary>
    /// Visual-only component added to each StageLane GameObject.
    /// Stores ordered world-space waypoints and provides path sampling via piecewise linear interpolation.
    /// </summary>
    public class LaneVisual : MonoBehaviour
    {
        [Tooltip("Ordered world-space waypoints. WP_0 = lane entry; last waypoint = lane exit. Auto-populated from Blender WP empties.")]
        public Transform[] waypoints = System.Array.Empty<Transform>();

        private void Awake()
        {
            if (waypoints.Length == 0)
                PopulateWaypoints();
        }

        #if UNITY_EDITOR
        private void OnValidate()
        {
            PopulateWaypoints();
        }
        #endif

        /// <summary>
        /// Returns the world-space position along the lane path at normalised t (0 = entry, 1 = exit).
        /// Linearly interpolates between waypoints; returns transform.position if no waypoints are set.
        /// </summary>
        public Vector3 GetPathPosition(float t)
        {
            t = Mathf.Clamp01(t);

            if (waypoints.Length >= 2)
                return SampleLinear(t);

            return transform.position;
        }

        /// <summary>
        /// Returns the forward tangent direction along the lane path at normalised t.
        /// Computed via finite difference.
        /// </summary>
        public Vector3 GetPathTangent(float t)
        {
            const float delta = 0.001f;
            float tA = Mathf.Clamp01(t - delta);
            float tB = Mathf.Clamp01(t + delta);
            Vector3 a = GetPathPosition(tA);
            Vector3 b = GetPathPosition(tB);
            Vector3 tangent = b - a;
                if (tangent.sqrMagnitude < 1e-8f)
                    return transform.forward;
            return tangent.normalized;
        }

        // ==================== PRIVATE ====================

        private void PopulateWaypoints()
        {
            var found = new SortedList<int, Transform>();

            foreach (Transform child in transform)
            {
                string upper = child.name.ToUpperInvariant();
                int wpPos = upper.LastIndexOf("WP");
                if (wpPos < 0) continue;
                int numStart = wpPos + 2;
                if (numStart < child.name.Length && child.name[numStart] == '_')
                    numStart++;
                if (int.TryParse(child.name[numStart..], out int index))
                    found[index] = child;
            }

            if (found.Count > 0)
                waypoints = new List<Transform>(found.Values).ToArray();
        }

        private Vector3 SampleLinear(float t)
        {
            int lastIndex = waypoints.Length - 1;
            float scaledT = t * lastIndex;
            int segmentIndex = Mathf.Min(Mathf.FloorToInt(scaledT), lastIndex - 1);
            float segmentT = scaledT - segmentIndex;

            Vector3 p1 = waypoints[segmentIndex].position;
            Vector3 p2 = waypoints[segmentIndex + 1].position;
            return Vector3.Lerp(p1, p2, segmentT);
        }
    }
}
