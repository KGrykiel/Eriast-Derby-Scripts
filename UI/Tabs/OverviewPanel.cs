using UnityEngine;
using TMPro;
using System.Linq;
using System.Collections.Generic;
using EventType = Assets.Scripts.Logging.EventType;
using Assets.Scripts.Logging;
using Assets.Scripts.Managers;
using Assets.Scripts.Stages;
using Assets.Scripts.Entities.Vehicles;
using Assets.Scripts.UI;
using Assets.Scripts.Managers.Turn;
using Assets.Scripts.Managers.Race;

public class OverviewPanel : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI leaderboardText;
    public TextMeshProUGUI eventSummaryText;

    [Header("Settings")]
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

    void OnEnable()
    {
        TurnEventBus.OnEvent += HandleTurnEvent;
    }

    void OnDisable()
    {
        TurnEventBus.OnEvent -= HandleTurnEvent;
    }

    private void HandleTurnEvent(TurnEvent evt)
    {
        if (evt is TurnEndedEvent || evt is RaceOverEvent)
            RefreshPanel();
    }

    public void RefreshPanel()
    {
        UpdateLeaderboard();
        UpdateEventSummary();
    }

    private void UpdateLeaderboard()
    {
        if (leaderboardText == null) return;

        if (stateMachine == null)
        {
            if (gameManager == null)
                gameManager = FindFirstObjectByType<GameManager>();
            if (gameManager != null)
                stateMachine = gameManager.GetStateMachine();
        }

        if (stateMachine == null) return;

        string display = "<b><size=18>=== RACE STANDINGS ===</size></b>\n";
        display += $"<color=#888888>Round {stateMachine.CurrentRound} | Phase: {stateMachine.CurrentPhase}</color>\n";
        display += BuildStageCountLine(stateMachine.AllVehicles) + "\n\n";

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

            var vStage = RacePositionTracker.GetStage(vehicle);
            var vLane = RacePositionTracker.GetLane(vehicle);
            string stageName = (vStage != null && !string.IsNullOrEmpty(vStage.stageName)) ? vStage.stageName : "Unknown";
            string laneName = vLane != null ? vLane.laneName : "-";
            float stageLength = vStage != null ? vStage.length : 0;
            display += $"   <color={LogColors.IconMovement}>{stageName} > {laneName}</color> ({RacePositionTracker.GetProgress(vehicle):F1}/{stageLength:F0}m)";

            if (showDistanceToFinish)
            {
                float distToFinish = -CalculateTotalProgress(vehicle);

                if (vStage != null && TrackDefinition.IsFinish(vStage))
                {
                    float remaining = vStage.length - RacePositionTracker.GetProgress(vehicle);
                    display += $" <color=#FFD700>[{remaining:F1}m to finish!]</color>";
                }
                else
                {
                    display += $" <color=#AAFFFF>[{distToFinish:F1}m to finish]</color>";
                }
            }

            if (showEnergy)
            {
                var powerCore = vehicle.PowerCore;
                int energy = powerCore != null ? powerCore.GetCurrentEnergy() : 0;
                int maxEnergy = powerCore != null ? powerCore.GetMaxEnergy() : 0;
                display += $" Energy:{energy}/{maxEnergy}";
            }

            if (showSpeed)
            {
                var drive = vehicle.Drive;
                float speed = drive != null ? drive.GetMaxSpeed() : 0f;
                display += $" Speed:{speed:F1}";
            }

            if (showModifiers)
            {
                var vehicleConditions = vehicle.GetActiveVehicleConditions();
                var componentConditions = vehicle.AllComponents
                    .SelectMany(c => c.GetActiveConditions())
                    .Select(c => c.ModifierLabel)
                    .Where(n => !string.IsNullOrEmpty(n));

                var allConditionNames = vehicleConditions
                    .Select(c => c.ModifierLabel)
                    .Concat(componentConditions)
                    .Where(n => !string.IsNullOrEmpty(n))
                    .ToList();

                if (allConditionNames.Count > 0)
                {
                    string pills = string.Join(" ", allConditionNames
                        .Select(n => $"<color={LogColors.IconCondition}>[{n}]</color>"));
                    display += $"\n   {pills}";
                }
            }

            string lastAction = GetLastAction(vehicle);
            if (!string.IsNullOrEmpty(lastAction))
                display += $"\n   <color={LogColors.ImportanceLow}>Last: {lastAction}</color>";

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
        var vStage = RacePositionTracker.GetStage(vehicle);
        if (vStage == null)
            return float.MinValue;

        if (TrackDefinition.Active == null)
            return -999999f;

        if (TrackDefinition.Active.IsFinishStage(vStage))
            return -(vStage.length - RacePositionTracker.GetProgress(vehicle));

        float shortestDistanceToFinish = TrackDefinition.Active.GetShortestDistanceToFinish(vStage, RacePositionTracker.GetProgress(vehicle));
        return -shortestDistanceToFinish;
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

    private string GetLastAction(Vehicle vehicle)
    {
        for (int i = RaceHistory.AllEvents.Count - 1; i >= 0; i--)
        {
            var evt = RaceHistory.AllEvents[i];
            if (evt.involvedVehicles.Contains(vehicle))
            {
                string desc = evt.description ?? "";
                return desc.Length > 50 ? desc.Substring(0, 47) + "..." : desc;
            }
        }
        return "";
    }

    private string BuildStageCountLine(IReadOnlyList<Vehicle> allVehicles)
    {
        var stageCounts = new Dictionary<string, int>();
        foreach (var v in allVehicles)
        {
            if (v.Status != VehicleStatus.Active) continue;
            var s = RacePositionTracker.GetStage(v);
            string key = (s != null && !string.IsNullOrEmpty(s.stageName)) ? s.stageName : "?";
            if (!stageCounts.ContainsKey(key)) stageCounts[key] = 0;
            stageCounts[key]++;
        }

        if (stageCounts.Count == 0) return "";

        string parts = string.Join("  ", stageCounts
            .Select(kv => $"<color={LogColors.InspectorHeader}>{kv.Key}</color>: {kv.Value}"));
        return $"<color={LogColors.ImportanceLow}>Stages: {parts}</color>";
    }

    }