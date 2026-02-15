using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using EventType = Assets.Scripts.Logging.EventType;
using Assets.Scripts.Logging;
using Assets.Scripts.Managers;
using Assets.Scripts.Stages;

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

    private GameManager gameManager;
    private TurnStateMachine stateMachine;

    void Start()
    {
        gameManager = FindFirstObjectByType<GameManager>();
        if (gameManager != null)
        {
            stateMachine = gameManager.GetStateMachine();
        }
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
        if (leaderboardText == null || stateMachine == null) return;

        string display = "<b><size=18>=== RACE STANDINGS ===</size></b>\n";
        display += $"<color=#888888>Round {stateMachine.CurrentRound} | Phase: {stateMachine.CurrentPhase}</color>\n\n";

        var activeVehicles = stateMachine.AllVehicles
            .Where(v => v.Status == VehicleStatus.Active)
            .OrderByDescending(v => CalculateTotalProgress(v))
            .ToList();

        for (int i = 0; i < activeVehicles.Count; i++)
        {
            var vehicle = activeVehicles[i];
            bool isCurrentTurn = stateMachine.CurrentVehicle == vehicle;

            string posIcon = GetPositionIcon(i + 1);
            string name = vehicle.vehicleName;

            if (isCurrentTurn)
                name = $"<color=yellow>> {name}</color>";

            if (vehicle.controlType == ControlType.Player)
                name = $"<b>{name}</b>";

            display += $"{posIcon} {name}\n";

            string stageName = vehicle.currentStage != null ? vehicle.currentStage.stageName : "Unknown";
            float stageLength = vehicle.currentStage != null ? vehicle.currentStage.length : 0;
            display += $"   {stageName} ({vehicle.progress:F1}/{stageLength:F0}m)";

            if (showDistanceToFinish)
            {
                float distToFinish = -CalculateTotalProgress(vehicle);

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

            if (showEnergy)
            {
                int energy = vehicle.powerCore?.GetCurrentEnergy() ?? 0;
                int maxEnergy = vehicle.powerCore?.GetMaxEnergy() ?? 0;
                display += $" Energy:{energy}/{maxEnergy}";
            }

            if (showSpeed)
            {
                float speed = vehicle.GetDriveComponent()?.GetMaxSpeed() ?? 0f;
                display += $" Speed:{speed:F1}";
            }

            if (showModifiers)
            {
                int statusCount = vehicle.AllComponents.Sum(c => c.GetActiveStatusEffects().Count);
                if (statusCount > 0)
                    display += $" Effects:x{statusCount}";
            }


            display += "\n";

            int health = vehicle.chassis != null ? vehicle.chassis.GetCurrentHealth() : 0;
            int maxHealth = vehicle.chassis != null ? vehicle.chassis.GetMaxHealth() : 1;
            float healthPercent = (float)health / maxHealth;
            string healthBar = GenerateCompactBar(healthPercent, 8);
            string healthColor = GetHealthColor(healthPercent);
            display += $"   <color={healthColor}>{healthBar}</color> <color=#AAAAAA>{health}/{maxHealth}HP</color>\n\n";
        }

        var destroyedVehicles = stateMachine.AllVehicles
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

        display += "\n<b>RACE STATISTICS:</b>\n";
        display += $"  Total Events: {RaceHistory.Instance.AllEvents.Count}\n";
        display += $"  Critical: {RaceHistory.Instance.AllEvents.Count(e => e.importance == EventImportance.Critical)}\n";
        display += $"  Combats: {RaceHistory.Instance.AllEvents.Count(e => e.type == EventType.Combat)}\n";
        display += $"  Destructions: {RaceHistory.Instance.AllEvents.Count(e => e.type == EventType.Destruction)}\n";

        eventSummaryText.text = display;
    }

    private float CalculateTotalProgress(Vehicle vehicle)
    {
        if (vehicle.currentStage == null)
            return float.MinValue;

        if (vehicle.currentStage.isFinishLine)
        {
            return -(vehicle.currentStage.length - vehicle.progress);
        }

        float shortestDistanceToFinish = FindShortestDistanceToFinish(vehicle.currentStage, vehicle.progress);
        return -shortestDistanceToFinish;
    }

    private float FindShortestDistanceToFinish(Stage currentStage, float currentProgress)
    {
        if (currentStage.isFinishLine && currentProgress < 1f)
            return CalculateFullLapDistance(currentStage);

        Queue<(Stage stage, float distance)> queue = new();
        HashSet<Stage> visited = new();

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
            return 999999f;
        }

        float shortestDistance = float.MaxValue;

        while (queue.Count > 0)
        {
            var (stage, distanceSoFar) = queue.Dequeue();

            if (stage.isFinishLine)
            {
                if (distanceSoFar < shortestDistance)
                    shortestDistance = distanceSoFar;
                continue;
            }

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

        return shortestDistance == float.MaxValue ? 999999f : shortestDistance;
    }

    private float CalculateFullLapDistance(Stage startFinishStage)
    {
        Queue<(Stage stage, float distance)> queue = new();
        HashSet<Stage> visited = new();

        visited.Add(startFinishStage);
        queue.Enqueue((startFinishStage, startFinishStage.length));

        float lapDistance = float.MaxValue;

        while (queue.Count > 0)
        {
            var (stage, distanceSoFar) = queue.Dequeue();

            if (stage.nextStages != null)
            {
                foreach (var nextStage in stage.nextStages)
                {
                    if (nextStage == startFinishStage)
                    {
                        if (distanceSoFar < lapDistance)
                            lapDistance = distanceSoFar;
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