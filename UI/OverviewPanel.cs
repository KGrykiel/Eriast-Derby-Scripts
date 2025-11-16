using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using RacingGame.Events;
using EventType = RacingGame.Events.EventType;

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

    private RaceLeaderboard raceLeaderboard;
    private TurnController turnController;

    void Start()
    {
        raceLeaderboard = FindObjectOfType<RaceLeaderboard>();
        turnController = FindObjectOfType<TurnController>();

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
            .OrderByDescending(v => CalculateProgress(v))
            .ToList();

        for (int i = 0; i < activeVehicles.Count; i++)
        {
            var vehicle = activeVehicles[i];
            bool isCurrentTurn = turnController.CurrentVehicle == vehicle;

            string posIcon = GetPositionIcon(i + 1);
            string name = vehicle.vehicleName;

            if (isCurrentTurn)
                name = $"<color=yellow>> {name}</color>"; // Changed from ►

            if (vehicle.controlType == ControlType.Player)
                name = $"<b>{name}</b>";

            display += $"{posIcon} {name}\n";

            // Location
            string stageName = vehicle.currentStage?.stageName ?? "Unknown";
            display += $"   {stageName} ({vehicle.progress:F1}m)\n";

            // Health bar (compact)
            float healthPercent = vehicle.health / vehicle.GetAttribute(Attribute.MaxHealth);
            string healthBar = GenerateCompactBar(healthPercent, 8);
            string healthColor = GetHealthColor(healthPercent);
            display += $"   <color={healthColor}>{healthBar}</color> HP\n\n";
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
                display += $"  X {vehicle.vehicleName}\n"; // Changed from ✕
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

    private float CalculateProgress(Vehicle vehicle)
    {
        // Simple progress calculation (you can reuse RaceLeaderboard logic if needed)
        return vehicle.progress;
    }

    private string GetPositionIcon(int position)
    {
        return position switch
        {
            1 => "[1st]", // Changed from medal emojis
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
            bar += i < filled ? "#" : "-"; // Changed from █ and ░
        }

        return bar;
    }
}