using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts.Stages.Lanes;

namespace Assets.Scripts.Stages
{
    /// <summary>
    /// Defines a designer-specified connection from a lane in one stage to a lane in another.
    /// Drag the prefab components directly — no string entry required.
    /// </summary>
    [System.Serializable]
    public class LaneLink
    {
        [Tooltip("The lane in the source stage.")]
        public StageLane fromLane;

        [Tooltip("The lane in the target stage.")]
        public StageLane toLane;
    }

    /// <summary>
    /// Single source of truth for course topology — explicit designer-specified lane-to-lane connections between stages.
    /// Assign this ScriptableObject asset to GameManager. All routing queries go through TrackDefinition.Active.
    /// </summary>
    [CreateAssetMenu(fileName = "TrackDefinition", menuName = "Racing/Track Definition")]
    public class TrackDefinition : ScriptableObject
    {
        // ==================== STAGE REGISTRY ====================
        // Stages self-register on OnEnable and deregister on OnDisable.

        private static readonly List<Stage> _registeredStages = new();

        public static void Register(Stage stage)
        {
            if (!_registeredStages.Contains(stage))
                _registeredStages.Add(stage);
        }

        public static void Unregister(Stage stage) => _registeredStages.Remove(stage);

        /// <summary>Returns a snapshot of all currently registered stages.</summary>
        public static List<Stage> GetAll() => new(_registeredStages);

        // ==================== TRACK DEFINITION ====================

        /// <summary>The track definition in use for the current race. Set by GameManager at initialization.</summary>
        public static TrackDefinition Active { get; private set; }

        [Tooltip("The stage where vehicles begin the race.")]
        public Stage entryStage;

        [Tooltip("The stage that constitutes the finish line for this track.")]
        public Stage finishStage;

        public List<LaneLink> transitions = new();

        // Pre-resolved at SetAsActive() time. All prefab-asset-to-scene-instance conversion lives here
        // and nowhere else. This exists only because StageCreator generates prefab-based content for
        // quick iteration; when content is built directly in the editor this becomes a direct assignment.
        private Dictionary<StageLane, StageLane> _resolvedLinks;
        private Stage _resolvedEntryStage;
        private Stage _resolvedFinishStage;

        private static Stage FindRegisteredByName(string stageName) =>
            _registeredStages.Find(s => s != null && s.stageName == stageName);

        /// <summary>
        /// Sets this definition as active and resolves all lane links to live scene instances.
        /// Must be called after all stages are loaded, before any routing queries run.
        /// </summary>
        public void SetAsActive()
        {
            Active = this;
            _resolvedLinks = new Dictionary<StageLane, StageLane>();
            _resolvedEntryStage  = entryStage  != null ? FindRegisteredByName(entryStage.stageName)  : null;
            _resolvedFinishStage = finishStage != null ? FindRegisteredByName(finishStage.stageName) : null;

            foreach (var link in transitions)
            {
                if (link.fromLane == null || link.toLane == null) continue;

                Stage fromPrefabStage = link.fromLane.GetComponentInParent<Stage>(true);
                Stage toPrefabStage   = link.toLane.GetComponentInParent<Stage>(true);
                if (fromPrefabStage == null || toPrefabStage == null) continue;

                Stage fromInstance = FindRegisteredByName(fromPrefabStage.stageName);
                Stage toInstance   = FindRegisteredByName(toPrefabStage.stageName);
                if (fromInstance == null || toInstance == null) continue;

                StageLane fromLaneInstance = fromInstance.lanes.Find(l => l != null && l.laneName == link.fromLane.laneName);
                StageLane toLaneInstance = toInstance.lanes.Find(l => l != null && l.laneName == link.toLane.laneName);
                if (fromLaneInstance == null || toLaneInstance == null) continue;

                _resolvedLinks[fromLaneInstance] = toLaneInstance;
            }
        }

        // ==================== ROUTING QUERIES ====================

        /// <summary>Returns the scene-instance stage where vehicles begin the race.</summary>
        public Stage GetEntryStage() => _resolvedEntryStage;

        /// <summary>Returns true if <paramref name="stage"/> is this track's finish line.</summary>
        public bool IsFinishStage(Stage stage) => stage != null && stage == _resolvedFinishStage;

        /// <summary>Null-safe static helper. Returns true if the active track defines <paramref name="stage"/> as the finish line.</summary>
        public static bool IsFinish(Stage stage)
        {
            if (Active == null || stage == null) return false;
            return Active.IsFinishStage(stage);
        }

        /// <summary>Returns the next stage for a vehicle leaving via <paramref name="fromLane"/>.</summary>
        public Stage GetNextStage(StageLane fromLane)
        {
            if (fromLane == null || _resolvedLinks == null) return null;
            if (!_resolvedLinks.TryGetValue(fromLane, out var toLane) || toLane == null) return null;
            return toLane.GetComponentInParent<Stage>();
        }

        /// <summary>Returns the target lane for a vehicle leaving via <paramref name="fromLane"/>.</summary>
        public StageLane GetTargetLane(StageLane fromLane)
        {
            if (fromLane == null || _resolvedLinks == null) return null;
            _resolvedLinks.TryGetValue(fromLane, out var toLane);
            return toLane;
        }

        /// <summary>Returns all unique stages reachable from <paramref name="fromStage"/> via any lane.</summary>
        public IEnumerable<Stage> GetConnectedStages(Stage fromStage)
        {
            if (fromStage == null || _resolvedLinks == null) yield break;
            var seen = new HashSet<Stage>();
            foreach (var lane in fromStage.lanes)
            {
                if (lane == null) continue;
                if (!_resolvedLinks.TryGetValue(lane, out var toLane) || toLane == null) continue;
                Stage nextStage = toLane.GetComponentInParent<Stage>();
                if (nextStage != null && seen.Add(nextStage))
                    yield return nextStage;
            }
        }

        /// <summary>
        /// Null-safe static helper. Returns connected stages from the active track definition,
        /// or an empty sequence if no definition is loaded.
        /// </summary>
        public static IEnumerable<Stage> GetConnected(Stage fromStage)
        {
            if (Active == null || fromStage == null) return System.Array.Empty<Stage>();
            return Active.GetConnectedStages(fromStage);
        }

        // ==================== DISTANCE QUERIES ====================

        /// <summary>
        /// Returns the shortest distance from <paramref name="fromStage"/> at <paramref name="fromProgress"/> to a finish line stage via BFS.
        /// Returns 999999 if no path to finish is found.
        /// </summary>
        public float GetShortestDistanceToFinish(Stage fromStage, float fromProgress)
        {
            if (fromStage == null) return 999999f;

            if (fromStage == _resolvedFinishStage && fromProgress < 1f)
                return GetFullLapDistance(fromStage);

            Queue<(Stage stage, float distance)> queue = new();
            HashSet<Stage> visited = new();

            float remainingInCurrent = fromStage.length - fromProgress;
            foreach (var nextStage in GetConnectedStages(fromStage))
            {
                visited.Add(nextStage);
                queue.Enqueue((nextStage, remainingInCurrent + nextStage.length));
            }

            if (queue.Count == 0)
                return 999999f;

            float shortestDistance = float.MaxValue;

            while (queue.Count > 0)
            {
                var (stage, distanceSoFar) = queue.Dequeue();

                if (stage == _resolvedFinishStage)
                {
                    if (distanceSoFar < shortestDistance)
                        shortestDistance = distanceSoFar;
                    continue;
                }

                foreach (var nextStage in GetConnectedStages(stage))
                {
                    if (!visited.Contains(nextStage))
                    {
                        visited.Add(nextStage);
                        queue.Enqueue((nextStage, distanceSoFar + nextStage.length));
                    }
                }
            }

            return shortestDistance == float.MaxValue ? 999999f : shortestDistance;
        }

        private float GetFullLapDistance(Stage startFinishStage)
        {
            if (startFinishStage == null) return 999999f;

            Queue<(Stage stage, float distance)> queue = new();
            HashSet<Stage> visited = new();

            visited.Add(startFinishStage);
            queue.Enqueue((startFinishStage, startFinishStage.length));

            float lapDistance = float.MaxValue;

            while (queue.Count > 0)
            {
                var (stage, distanceSoFar) = queue.Dequeue();

                foreach (var nextStage in GetConnectedStages(stage))
                {
                    if (nextStage == startFinishStage)
                    {
                        if (distanceSoFar < lapDistance)
                            lapDistance = distanceSoFar;
                    }
                    else if (!visited.Contains(nextStage))
                    {
                        visited.Add(nextStage);
                        queue.Enqueue((nextStage, distanceSoFar + nextStage.length));
                    }
                }
            }

            // If no loop found, return a large number
            return lapDistance == float.MaxValue ? 999999f : lapDistance;
        }
    }
}

