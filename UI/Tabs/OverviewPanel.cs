using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using EventType = Assets.Scripts.Logging.EventType;
using Assets.Scripts.Logging;

/// <summary>
/// Overview panel showing race leaderboard and event summary.
/// Provides high-level view of race state.
/// </summary>
public class OverviewPanel : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI leaderboardText;
    public TextMeshProUGUI eventSummaryText;

    [Header("Settings")]
    public bool autoRefresh = true;
    public int criticalEventCount = 10;

    [Header("Display Settings")]
    [Tooltip("Show distance to finish line")]
    public bool showDistanceToFinish = true;
    
    [Tooltip("Show energy for each vehicle")]
    public bool showEnergy = true;
    
    [Tooltip("Show speed stat")]
    public bool showSpeed = false;
    
    [Tooltip("Show active modifier count")]
    public bool showModifiers = false;

    private TurnController turnController;

    void Start()
    {
        turnController = FindFirstObjectByType<TurnController>();
        RefreshPanel();
    }

    void Update()
    {
        if (autoRefresh && gameObject.activeInHierarchy)
        {
            RefreshPanel();
        }
    }

    public void RefreshPanel()
    {
        UpdateLeaderboard();
        UpdateEventSummary();
    }

    private void UpdateLeaderboard()
    {
        if (leaderboardText == null || turnController == null) return;

        // Build custom leaderboard
        string display = "<b><size=18>=== RACE STANDINGS ===</size></b>\n";
        display += "<color=#888888>------------------------</color>\n\n";

        var activeVehicles = turnController.AllVehicles
            .Where(v => v.Status == VehicleStatus.Active)
            .OrderByDescending(v => CalculateTotalProgress(v))
            .ToList();

        for (int i = 0; i < activeVehicles.Count; i++)
        {
            var vehicle = activeVehicles[i];
            bool isCurrentTurn = turnController.CurrentVehicle == vehicle;

            string posIcon = GetPositionIcon(i + 1);
            string name = vehicle.vehicleName;

            if (isCurrentTurn)
                name = $"<color=yellow>> {name}</color>";

            if (vehicle.controlType == ControlType.Player)
                name = $"<b>{name}</b>";

            display += $"{posIcon} {name}\n";

            // Location
            string stageName = vehicle.currentStage?.stageName ?? "Unknown";
            float stageLength = vehicle.currentStage?.length ?? 0;
            display += $"   {stageName} ({vehicle.progress:F1}/{stageLength:F0}m)";

            // Distance to finish
            if (showDistanceToFinish)
            {
                float distToFinish = -CalculateTotalProgress(vehicle); // Negate to get positive distance
                
                if (vehicle.currentStage != null && vehicle.currentStage.isFinishLine)
                {
                    float remaining = vehicle.currentStage.length - vehicle.progress;
                    display += $" <color=#FFD700>[{remaining:F1}m to finish!]</color>";
                }
                else
                {
                    display += $" <color=#AAFFFF>[{distToFinish:F1}m to finish]</color>";
                }
            }

            // Energy (inline)
            if (showEnergy)
            {
                int energy = vehicle.powerCore?.GetCurrentEnergy() ?? 0;
                int maxEnergy = vehicle.powerCore?.GetMaxEnergy() ?? 0;
                display += $" Energy:{energy}/{maxEnergy}";
            }

            // Speed (inline)
            if (showSpeed)
            {
                float speed = vehicle.GetDriveComponent()?.GetMaxSpeed() ?? 0f;
                display += $" Speed:{speed:F1}";
            }

            // Status effect count (inline)
            if (showModifiers)
            {
                int statusCount = vehicle.AllComponents.Sum(c => c.GetActiveStatusEffects().Count);
                if (statusCount > 0)
                    display += $" Effects:x{statusCount}";
            }

            display += "\n";

            // Health bar (compact)
            int health = vehicle.chassis?.GetCurrentHealth() ?? 0;
            int maxHealth = vehicle.chassis?.GetMaxHealth() ?? 1;
            float healthPercent = (float)health / maxHealth;
            string healthBar = GenerateCompactBar(healthPercent, 8);
            string healthColor = GetHealthColor(healthPercent);
            display += $"   <color={healthColor}>{healthBar}</color> <color=#AAAAAA>{health}/{maxHealth}HP</color>\n\n";
        }

        // Destroyed vehicles
        var destroyedVehicles = turnController.AllVehicles
            .Where(v => v.Status == VehicleStatus.Destroyed)
            .ToList();

        if (destroyedVehicles.Count > 0)
        {
            display += "<color=#FF4444><b>ELIMINATED:</b></color>\n";
            foreach (var vehicle in destroyedVehicles)
            {
                display += $"  X {vehicle.vehicleName}\n";
            }
        }

        leaderboardText.text = display;
    }

    private void UpdateEventSummary()
    {
        if (eventSummaryText == null) return;

        string display = "<b><size=18>=== CRITICAL EVENTS ===</size></b>\n";
        display += "<color=#888888>------------------------</color>\n\n";

        // Get critical and high importance events
        var criticalEvents = RaceHistory.Instance.AllEvents
            .Where(e => e.importance <= EventImportance.High)
            .TakeLast(criticalEventCount)
            .ToList();

        if (criticalEvents.Count == 0)
        {
            display += "<color=#888888>No critical events yet</color>";
        }
        else
        {
            foreach (var evt in criticalEvents)
            {
                display += evt.GetFormattedText(includeTimestamp: true, includeLocation: true) + "\n\n";
            }
        }

        // Add statistics
        display += "\n<b>RACE STATISTICS:</b>\n";
        display += $"  Total Events: {RaceHistory.Instance.AllEvents.Count}\n";
        display += $"  Critical: {RaceHistory.Instance.AllEvents.Count(e => e.importance == EventImportance.Critical)}\n";
        display += $"  Combats: {RaceHistory.Instance.AllEvents.Count(e => e.type == EventType.Combat)}\n";
        display += $"  Destructions: {RaceHistory.Instance.AllEvents.Count(e => e.type == EventType.Destruction)}\n";

        eventSummaryText.text = display;
    }

    // Helper methods

    /// <summary>
    /// Calculates how close a vehicle is to finishing the race.
    /// Lower values = closer to finish = higher rank.
    /// Handles branching paths and circuit races where start = finish.
    /// Uses same logic as RaceLeaderboard for consistency.
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

    private string GetPositionIcon(int position)
    {
        return position switch
        {
            1 => "[1st]",
            2 => "[2nd]",
            3 => "[3rd]",
            _ => $"[{position}]"
        };
    }

    private string GetHealthColor(float percent)
    {
        if (percent > 0.6f) return "#44FF44";
        if (percent > 0.3f) return "#FFFF44";
        return "#FF4444";
    }

    private string GenerateCompactBar(float percent, int length)
    {
        int filled = Mathf.RoundToInt(percent * length);
        filled = Mathf.Clamp(filled, 0, length);

        string bar = "";
        for (int i = 0; i < length; i++)
        {
            bar += i < filled ? "#" : "-";
        }

        return bar;
    }
}