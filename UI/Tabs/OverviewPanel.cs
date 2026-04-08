using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using EventType = Assets.Scripts.Logging.EventType;
using Assets.Scripts.Logging;
using Assets.Scripts.Managers;
using Assets.Scripts.Stages;
using Assets.Scripts.Entities.Vehicles;
using Assets.Scripts.UI;

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

            string stageName = vehicle.CurrentStage != null ? vehicle.CurrentStage.stageName : "Unknown";
            float stageLength = vehicle.CurrentStage != null ? vehicle.CurrentStage.length : 0;
            display += $"   {stageName} ({vehicle.Progress:F1}/{stageLength:F0}m)";

            if (showDistanceToFinish)
            {
                float distToFinish = -CalculateTotalProgress(vehicle);

                if (vehicle.CurrentStage != null && vehicle.CurrentStage.isFinishLine)
                {
                    float remaining = vehicle.CurrentStage.length - vehicle.Progress;
                    display += $" <color=#FFD700>[{remaining:F1}m to finish!]</color>";
                }
                else
                {
                    display += $" <color=#AAFFFF>[{distToFinish:F1}m to finish]</color>";
                }
            }

            if (showEnergy)
            {
                int energy = vehicle.PowerCore?.GetCurrentEnergy() ?? 0;
                int maxEnergy = vehicle.PowerCore?.GetMaxEnergy() ?? 0;
                display += $" Energy:{energy}/{maxEnergy}";
            }

            if (showSpeed)
            {
                float speed = vehicle.Drive?.GetMaxSpeed() ?? 0f;
                display += $" Speed:{speed:F1}";
            }

            if (showModifiers)
            {
                int statusCount = vehicle.AllComponents.Sum(c => c.GetActiveConditions().Count);
                if (statusCount > 0)
                    display += $" Effects:x{statusCount}";
            }


            display += "\n";

            int health = vehicle.Chassis != null ? vehicle.Chassis.GetCurrentHealth() : 0;
            int maxHealth = vehicle.Chassis != null ? vehicle.Chassis.GetMaxHealth() : 1;
            float healthPercent = (float)health / maxHealth;
            string healthBar = UITextHelpers.GenerateBar(healthPercent, 8);
            string healthColor = UITextHelpers.GetHealthColor(healthPercent);
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

        var criticalEvents = RaceHistory.AllEvents
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
        display += $"  Total Events: {RaceHistory.AllEvents.Count}\n";
        display += $"  Critical: {RaceHistory.AllEvents.Count(e => e.importance == EventImportance.Critical)}\n";
        display += $"  Combats: {RaceHistory.AllEvents.Count(e => e.type == EventType.Combat)}\n";
        display += $"  Destructions: {RaceHistory.AllEvents.Count(e => e.type == EventType.Destruction)}\n";

        eventSummaryText.text = display;
    }

    private float CalculateTotalProgress(Vehicle vehicle)
    {
        if (vehicle.CurrentStage == null)
            return float.MinValue;

        if (vehicle.CurrentStage.isFinishLine)
        {
            return -(vehicle.CurrentStage.length - vehicle.Progress);
        }

        float shortestDistanceToFinish = FindShortestDistanceToFinish(vehicle.CurrentStage, vehicle.Progress);
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

    }