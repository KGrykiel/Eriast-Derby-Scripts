using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Displays a live leaderboard of vehicle positions in the race.
/// Updates on-demand when turns advance (not every frame).
/// Supports both point-to-point and circuit races (where start = finish).
/// </summary>
public class RaceLeaderboard : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("TextMeshPro component to display leaderboard")]
    public TextMeshProUGUI leaderboardText;

    [Tooltip("Reference to TurnController (assign in Inspector)")]
    public TurnController turnController;

    [Header("Display Settings")]
    [Tooltip("Show health bars for each vehicle")]
    public bool showHealth = true;

    [Tooltip("Show energy for each vehicle")]
    public bool showEnergy = true;

    [Tooltip("Show active modifier count")]
    public bool showModifiers = false;

    [Tooltip("Show speed stat")]
    public bool showSpeed = false;

    [Tooltip("Show distance to finish line")]
    public bool showDistanceToFinish = true;

    private void Start()
    {
        // Auto-find TurnController if not assigned
        if (turnController == null)
        {
            var gameManager = FindFirstObjectByType<GameManager>();
            if (gameManager != null)
            {
                // Wait one frame for GameManager to create TurnController
                StartCoroutine(InitializeAfterFrame());
            }
        }
        else
        {
            RefreshLeaderboard();
        }
    }

    private System.Collections.IEnumerator InitializeAfterFrame()
    {
        yield return null; // Wait one frame
        
        turnController = FindFirstObjectByType<TurnController>();
        
        if (turnController == null)
        {
            Debug.LogError("[RaceLeaderboard] Could not find TurnController!");
        }
        else
        {
            RefreshLeaderboard();
        }
    }

    /// <summary>
    /// Call this method whenever the turn state changes.
    /// Should be called by GameManager/TurnController after each turn.
    /// </summary>
    public void RefreshLeaderboard()
    {
        if (leaderboardText == null)
        {
            Debug.LogWarning("[RaceLeaderboard] LeaderboardText is not assigned!");
            return;
        }

        if (turnController == null)
        {
            Debug.LogWarning("[RaceLeaderboard] TurnController is not assigned!");
            return;
        }

        string display = BuildLeaderboardDisplay();
        leaderboardText.text = display;
    }

    /// <summary>
    /// Generates the formatted leaderboard text.
    /// </summary>
    private string BuildLeaderboardDisplay()
    {
        // Header
        string display = "<b><size=20>=== RACE STANDINGS ===</size></b>\n";
        display += "<color=#888888>--------------------------</color>\n\n";

        // Get active vehicles sorted by progress
        var activeVehicles = turnController.AllVehicles
            .Where(v => v.Status == VehicleStatus.Active)
            .OrderByDescending(v => CalculateTotalProgress(v))
            .ToList();

        // Display each vehicle
        for (int i = 0; i < activeVehicles.Count; i++)
        {
            display += BuildVehicleEntry(activeVehicles[i], i + 1);
            display += "\n";
        }

        // Show destroyed vehicles (if any)
        var destroyedVehicles = turnController.AllVehicles
            .Where(v => v.Status == VehicleStatus.Destroyed)
            .ToList();

        if (destroyedVehicles.Count > 0)
        {
            display += "<color=#FF4444><b>*** ELIMINATED ***</b></color>\n\n";

            foreach (var vehicle in destroyedVehicles)
            {
                display += $"   X {vehicle.vehicleName}\n";
            }
        }

        return display;
    }

    /// <summary>
    /// Builds the display string for a single vehicle entry.
    /// </summary>
    private string BuildVehicleEntry(Vehicle vehicle, int position)
    {
        bool isCurrentTurn = turnController.CurrentVehicle == vehicle;

        // Position indicator
        string positionIcon = GetPositionEmoji(position);
        string positionText = $"{positionIcon} <b>#{position}</b>";

        // Vehicle name (highlight if current turn)
        string nameDisplay = isCurrentTurn
            ? $"<color=yellow>► {vehicle.vehicleName}</color>"
            : vehicle.vehicleName;

        // Build entry line by line
        string entry = $"{positionText} {nameDisplay}\n";

        // Location info
        string stageName = vehicle.currentStage != null ? vehicle.currentStage.stageName : "Unknown";
        float progress = vehicle.progress;
        float stageLength = vehicle.currentStage != null ? vehicle.currentStage.length : 0;
        entry += $"   Location: {stageName} ({progress:F1}/{stageLength:F0}m)";

        // Distance to finish (new!)
        if (showDistanceToFinish)
        {
            float distToFinish = -CalculateTotalProgress(vehicle); // Negate to get positive distance
            
            if (vehicle.currentStage != null && vehicle.currentStage.isFinishLine)
            {
                float remaining = vehicle.currentStage.length - vehicle.progress;
                entry += $" <color=#FFD700>[{remaining:F1}m to finish!]</color>";
            }
            else
            {
                entry += $" <color=#AAFFFF>[{distToFinish:F1}m to finish]</color>";
            }
        }

        // Energy (inline)
        if (showEnergy)
        {
            int maxEnergy = (int)vehicle.GetAttribute(Attribute.MaxEnergy);
            entry += $" Energy:{vehicle.energy}/{maxEnergy}";
        }

        // Speed (inline)
        if (showSpeed)
        {
            float speed = vehicle.GetAttribute(Attribute.Speed);
            entry += $" Speed:{speed:F1}";
        }

        // Modifier count (inline)
        if (showModifiers)
        {
            int modCount = vehicle.GetActiveModifiers().Count;
            if (modCount > 0)
                entry += $" Buffs:x{modCount}";
        }

        entry += "\n";

        // Health bar (new line)
        if (showHealth)
        {
            float maxHealth = vehicle.GetAttribute(Attribute.MaxHealth);
            float healthPercent = vehicle.health / maxHealth;
            string healthBar = GetHealthBar(healthPercent);
            entry += $"   {healthBar} <color=#AAAAAA>{vehicle.health}/{maxHealth:F0}HP</color>\n";
        }

        return entry;
    }

    /// <summary>
    /// Calculates how close a vehicle is to finishing the race.
    /// Lower values = closer to finish = higher rank.
    /// Handles branching paths and circuit races where start = finish.
    /// </summary>
    private float CalculateTotalProgress(Vehicle vehicle)
    {
        if (vehicle.currentStage == null)
            return float.MinValue; // No stage = furthest back

        // Check if vehicle is AT the finish line
        if (vehicle.currentStage.isFinishLine)
        {
            // Vehicle at finish: distance remaining = (stage length - progress)
            // Multiply by -1 so more progress = higher rank
            return -(vehicle.currentStage.length - vehicle.progress);
        }

        // Calculate shortest distance to ANY finish line using BFS
        float shortestDistanceToFinish = FindShortestDistanceToFinish(vehicle.currentStage, vehicle.progress);

        // Negate so LOWER distance = HIGHER rank value (for descending sort)
        return -shortestDistanceToFinish;
    }

    /// <summary>
    /// Uses Breadth-First Search to find shortest path to any finish line.
    /// Returns total distance from vehicle's current position to nearest finish.
    /// Handles circuit races where finish line is ahead via nextStages traversal.
    /// Special case: If on finish line at race start, calculates full lap distance.
    /// </summary>
    private float FindShortestDistanceToFinish(Stage currentStage, float currentProgress)
    {
        // Special case: If we're on a finish line stage with very little progress,
        // we're likely at the START of the race, not about to finish
        // Calculate the full lap distance by traversing forward
        if (currentStage.isFinishLine && currentProgress < 1f)
        {
            // At race start on finish line - need to complete full lap
            return CalculateFullLapDistance(currentStage);
        }

        // BFS to find shortest path to finish
        Queue<(Stage stage, float distance)> queue = new Queue<(Stage, float)>();
        HashSet<Stage> visited = new HashSet<Stage>();

        // Start BFS from NEXT stages (not current stage)
        // This prevents immediately detecting the start/finish line we're already on
        float remainingInCurrentStage = currentStage.length - currentProgress;
        
        if (currentStage.nextStages != null && currentStage.nextStages.Count > 0)
        {
            foreach (var nextStage in currentStage.nextStages)
            {
                if (nextStage != null)
                {
                    visited.Add(nextStage);
                    queue.Enqueue((nextStage, remainingInCurrentStage + nextStage.length));
                }
            }
        }
        else
        {
            // Dead end - no next stages
            return 999999f;
        }

        float shortestDistance = float.MaxValue;

        while (queue.Count > 0)
        {
            var (stage, distanceSoFar) = queue.Dequeue();

            // Check if this stage is a finish line
            if (stage.isFinishLine)
            {
                // Found a finish line! Update shortest distance
                if (distanceSoFar < shortestDistance)
                {
                    shortestDistance = distanceSoFar;
                }
                continue; // Don't explore beyond finish line
            }

            // Explore next stages
            if (stage.nextStages != null)
            {
                foreach (var nextStage in stage.nextStages)
                {
                    if (nextStage != null && !visited.Contains(nextStage))
                    {
                        visited.Add(nextStage);
                        float newDistance = distanceSoFar + nextStage.length;
                        queue.Enqueue((nextStage, newDistance));
                    }
                }
            }
        }

        // If no finish line found, return a very large number
        return shortestDistance == float.MaxValue ? 999999f : shortestDistance;
    }

    /// <summary>
    /// Calculates the full lap distance by traversing from start/finish line
    /// all the way around back to the start/finish line.
    /// Used when vehicles are at race start.
    /// </summary>
    private float CalculateFullLapDistance(Stage startFinishStage)
    {
        Queue<(Stage stage, float distance)> queue = new Queue<(Stage, float)>();
        HashSet<Stage> visited = new HashSet<Stage>();

        // Start from the finish line stage itself
        visited.Add(startFinishStage);
        queue.Enqueue((startFinishStage, startFinishStage.length));

        float lapDistance = float.MaxValue;

        while (queue.Count > 0)
        {
            var (stage, distanceSoFar) = queue.Dequeue();

            // Check if we've looped back to the start/finish
            if (stage.nextStages != null)
            {
                foreach (var nextStage in stage.nextStages)
                {
                    if (nextStage == startFinishStage)
                    {
                        // Found the loop back to start! This is the lap distance
                        if (distanceSoFar < lapDistance)
                        {
                            lapDistance = distanceSoFar;
                        }
                    }
                    else if (nextStage != null && !visited.Contains(nextStage))
                    {
                        visited.Add(nextStage);
                        float newDistance = distanceSoFar + nextStage.length;
                        queue.Enqueue((nextStage, newDistance));
                    }
                }
            }
        }

        // If no loop found, return a large number
        return lapDistance == float.MaxValue ? 999999f : lapDistance;
    }

    /// <summary>
    /// Returns a position indicator for the given rank.
    /// </summary>
    private string GetPositionEmoji(int position)
    {
        return position switch
        {
            1 => "[1st]",
            2 => "[2nd]",
            3 => "[3rd]",
            _ => "[" + position + "th]"
        };
    }

    /// <summary>
    /// Generates a visual health bar with color coding.
    /// </summary>
    private string GetHealthBar(float healthPercent)
    {
        int barLength = 10;
        int filled = Mathf.RoundToInt(healthPercent * barLength);
        filled = Mathf.Clamp(filled, 0, barLength);

        // Build bar
        string bar = "[";
        for (int i = 0; i < barLength; i++)
        {
            bar += i < filled ? "█" : "░";
        }
        bar += "]";

        // Color based on health percentage
        if (healthPercent > 0.6f)
            return $"<color=#44FF44>{bar}</color>"; // Green
        else if (healthPercent > 0.3f)
            return $"<color=#FFFF44>{bar}</color>"; // Yellow
        else
            return $"<color=#FF4444>{bar}</color>"; // Red
    }
}