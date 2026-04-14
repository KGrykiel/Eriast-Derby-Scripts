using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Visualisation
{
    /// <summary>
    /// Visual-only component added to each StageLane GameObject.
    /// Stores ordered world-space waypoints and provides path sampling via Catmull-Rom spline.
    /// When no waypoints are set, falls back to a straight-line lerp between injected fallback endpoints.
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
        /// Uses Catmull-Rom spline over waypoints when populated; returns transform.position if no waypoints are set.
        /// </summary>
        public Vector3 GetPathPosition(float t)
        {
            t = Mathf.Clamp01(t);

            if (waypoints.Length >= 2)
                return SampleCatmullRom(t);

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
                if (int.TryParse(child.name.Substring(numStart), out int index))
                    found[index] = child;
            }

            if (found.Count > 0)
                waypoints = new List<Transform>(found.Values).ToArray();
        }

        private Vector3 SampleCatmullRom(float t)
        {
            int lastIndex = waypoints.Length - 1;
            float scaledT = t * lastIndex;
            int segmentIndex = Mathf.Min(Mathf.FloorToInt(scaledT), lastIndex - 1);
            float segmentT = scaledT - segmentIndex;

            // Phantom endpoint trick: clamp indices at segment boundaries
            Vector3 p0 = waypoints[Mathf.Max(segmentIndex - 1, 0)].position;
            Vector3 p1 = waypoints[segmentIndex].position;
            Vector3 p2 = waypoints[Mathf.Min(segmentIndex + 1, lastIndex)].position;
            Vector3 p3 = waypoints[Mathf.Min(segmentIndex + 2, lastIndex)].position;

            return CatmullRom(p0, p1, p2, p3, segmentT);
        }

        private static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            float t2 = t * t;
            float t3 = t2 * t;
            return 0.5f * (
                2f * p1
                + (-p0 + p2) * t
                + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2
                + (-p0 + 3f * p1 - 3f * p2 + p3) * t3
            );
        }
    }
}
