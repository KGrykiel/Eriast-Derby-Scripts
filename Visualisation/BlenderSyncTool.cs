#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Stage = Assets.Scripts.Stages.Stage;
using Assets.Scripts.Stages.Lanes;

namespace Assets.Scripts.Visualisation
{
    /// <summary>
    /// Editor tool: reads an imported Blender model and pushes spatial data into the active scene.
    /// Stage anchor empties (Stage_{name}) reposition scene Stage GOs.
    /// Waypoint empties populate LaneVisual.waypoints[] — two formats supported:
    ///   Nested:  {LaneName}_WP{n}  as a direct child of the stage anchor Empty.
    ///   Flat:    Stage_{stage}_{lane}_WP{n}  anywhere in the hierarchy.
    /// Run via Assets/Racing/Sync From Blender Model after saving the .blend or reimporting the FBX.
    /// </summary>
    public static class BlenderSyncTool
    {
        [MenuItem("Assets/Racing/Sync From Blender Model")]
        public static void SyncFromBlenderModel()
        {
            GameObject modelRoot = FindBlenderModel();
            if (modelRoot == null)
            {
                EditorUtility.DisplayDialog(
                    "Sync From Blender Model",
                    "No track model found.\n\nSelect the imported FBX asset in the Project view and try again, or ensure it contains Stage_* objects.",
                    "OK");
                return;
            }

            Stage[] sceneStages = Object.FindObjectsByType<Stage>(FindObjectsSortMode.None);
            if (sceneStages.Length == 0)
            {
                Debug.LogWarning("[BlenderSyncTool] No Stage objects found in the current scene.");
                return;
            }

            // Build normalised name lookups
            var stageByNorm = new Dictionary<string, Stage>();
            var laneByNorm  = new Dictionary<(string, string), StageLane>();
            foreach (Stage s in sceneStages)
            {
                string normStage = Normalize(s.stageName);
                stageByNorm[normStage] = s;
                foreach (StageLane lane in s.lanes)
                {
                    if (lane == null) continue;
                    laneByNorm[(normStage, Normalize(lane.laneName))] = lane;
                }
            }

            // Collect every transform in the model hierarchy
            var allTransforms = new List<Transform>();
            CollectAll(modelRoot.transform, allTransforms);

            // First pass: stage anchors + raw WP position collection
            var waypointData = new Dictionary<(string, string), SortedDictionary<int, Vector3>>();
            int stagesMoved = 0;

            foreach (Transform t in allTransforms)
            {
                string objName = t.gameObject.name;

                // WP object: ends in _WP{n} regardless of prefix
                Match wpMatch = Regex.Match(objName, @"_WP(\d+)$", RegexOptions.IgnoreCase);
                if (wpMatch.Success)
                {
                    int    wpIndex   = int.Parse(wpMatch.Groups[1].Value);
                    string lanesPart = objName.Substring(0, wpMatch.Index);

                    string normStage = null;
                    string normLane  = null;
                    bool   resolved  = false;

                    bool parentIsStage = t.parent != null && t.parent.name.StartsWith("Stage_", System.StringComparison.OrdinalIgnoreCase);
                    bool selfIsStage   = objName.StartsWith("Stage_", System.StringComparison.OrdinalIgnoreCase);

                    // Nested short format: MainStreet_WP0 as a direct child of Stage_RuinedCity
                    if (parentIsStage && !selfIsStage)
                    {
                        normStage = Normalize(t.parent.name.Substring(6));
                        normLane  = Normalize(lanesPart);
                        resolved  = laneByNorm.ContainsKey((normStage, normLane));
                        if (!resolved)
                            Debug.LogWarning($"[BlenderSyncTool] No lane match for '{objName}' under '{t.parent.name}'.");
                    }
                    // Flat full format: Stage_RuinedCity_MainStreet_WP0
                    else if (selfIsStage)
                    {
                        string middle = objName.Substring(6, wpMatch.Index - 6);
                        resolved = TryMatchLane(middle, laneByNorm, out normStage, out normLane);
                        if (!resolved)
                            Debug.LogWarning($"[BlenderSyncTool] No stage/lane match for '{objName}'.");
                    }

                    if (resolved)
                    {
                        var key = (normStage, normLane);
                        if (!waypointData.ContainsKey(key))
                            waypointData[key] = new SortedDictionary<int, Vector3>();
                        waypointData[key][wpIndex] = t.position;
                    }
                    continue;
                }

                // Stage anchor: Stage_{name} with no WP suffix
                if (!objName.StartsWith("Stage_", System.StringComparison.OrdinalIgnoreCase))
                    continue;

                string normStageName = Normalize(objName.Substring(6));
                if (stageByNorm.TryGetValue(normStageName, out Stage stage))
                {
                    Undo.RecordObject(stage.transform, "Blender Sync");
                    stage.transform.position = t.position;
                    EditorUtility.SetDirty(stage.gameObject);
                    stagesMoved++;
                }
                else
                {
                    Debug.LogWarning($"[BlenderSyncTool] No scene Stage found for '{objName}'.");
                }
            }

            // Second pass: apply collected waypoints to LaneVisuals
            int waypointsSet = 0;
            foreach (Stage stage in sceneStages)
            {
                string normStage = Normalize(stage.stageName);
                foreach (StageLane lane in stage.lanes)
                {
                    if (lane == null) continue;
                    var key = (normStage, Normalize(lane.laneName));
                    if (!waypointData.TryGetValue(key, out SortedDictionary<int, Vector3> wpPositions))
                        continue;

                    LaneVisual lv = lane.GetComponent<LaneVisual>();
                    if (lv == null)
                    {
                        Debug.LogWarning($"[BlenderSyncTool] LaneVisual missing on '{lane.laneName}' in '{stage.stageName}'.");
                        continue;
                    }

                    // Clear existing WP_ children
                    for (int i = lane.transform.childCount - 1; i >= 0; i--)
                    {
                        Transform child = lane.transform.GetChild(i);
                        if (Regex.IsMatch(child.name, @"^WP_\d+$"))
                            Undo.DestroyObjectImmediate(child.gameObject);
                    }

                    // Create new WP children in ascending index order
                    var newWaypoints = new List<Transform>();
                    foreach (KeyValuePair<int, Vector3> kvp in wpPositions)
                    {
                        GameObject wpGO = new($"WP_{kvp.Key}");
                        Undo.RegisterCreatedObjectUndo(wpGO, "Blender Sync");
                        wpGO.transform.SetParent(lane.transform);
                        wpGO.transform.position = kvp.Value;
                        newWaypoints.Add(wpGO.transform);
                    }

                    Undo.RecordObject(lv, "Blender Sync");
                    lv.waypoints = newWaypoints.ToArray();
                    EditorUtility.SetDirty(lv);
                    waypointsSet += newWaypoints.Count;
                }
            }

            // Redraw all StageVisual LineRenderers
            foreach (Stage stage in sceneStages)
            {
                StageVisual sv = stage.GetComponent<StageVisual>();
                if (sv != null)
                {
                    sv.Refresh();
                    EditorUtility.SetDirty(sv);
                }
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log($"[BlenderSyncTool] Sync complete. Stages moved: {stagesMoved}. Waypoints set: {waypointsSet}.");
        }

        // ==================== HELPERS ====================

        private static GameObject FindBlenderModel()
        {
            // Prefer explicit user selection from the Project view
            GameObject selected = Selection.activeGameObject;
            if (selected != null && AssetDatabase.Contains(selected))
                return selected;

            // Auto-scan: first model asset with at least one Stage_ child
            string[] guids = AssetDatabase.FindAssets("t:Model");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.StartsWith("Assets/")) continue;

                GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (model == null) continue;

                foreach (Transform child in model.transform)
                {
                    if (child.name.StartsWith("Stage_", System.StringComparison.OrdinalIgnoreCase))
                        return model;
                }
            }

            return null;
        }

        private static void CollectAll(Transform parent, List<Transform> results)
        {
            foreach (Transform child in parent)
            {
                results.Add(child);
                CollectAll(child, results);
            }
        }

        /// <summary>
        /// Tries every underscore split of <paramref name="middle"/> against the known lane lookup.
        /// Handles camelCase and multi-word names normalised identically on both sides.
        /// </summary>
        private static bool TryMatchLane(
            string middle,
            Dictionary<(string, string), StageLane> laneByNorm,
            out string normStage,
            out string normLane)
        {
            string[] parts = middle.Split('_');
            for (int split = 1; split < parts.Length; split++)
            {
                normStage = Normalize(string.Join("_", parts, 0, split));
                normLane  = Normalize(string.Join("_", parts, split, parts.Length - split));
                if (laneByNorm.ContainsKey((normStage, normLane)))
                    return true;
            }

            normStage = null;
            normLane  = null;
            return false;
        }

        private static string Normalize(string name)
            => name.ToLowerInvariant().Replace(" ", "").Replace("_", "");
    }
}
#endif
