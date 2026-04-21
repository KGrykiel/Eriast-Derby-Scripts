using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Logging;
using Assets.Scripts.Managers;
using Assets.Scripts.Conditions.EntityConditions;
using Assets.Scripts.Entities.Vehicles;
using Assets.Scripts.UI;
using Assets.Scripts.Managers.Race;

public class FocusPanel : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI playerStatusText;
    public TextMeshProUGUI sameStageVehiclesText;
    public TextMeshProUGUI recentEventsText;
    
    [Header("Settings")]
    public int recentEventCount = 5;
    public bool autoRefresh = true;
    
    private GameManager gameManager;
    private Vehicle playerVehicle;
    
    
    void Start()
    {
        gameManager = FindFirstObjectByType<GameManager>();

        if (gameManager != null)
        {
            var playerVehicles = gameManager.GetPlayerVehicles();
            playerVehicle = playerVehicles.Count > 0 ? playerVehicles[0] : null;
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
        if (gameManager != null)
        {
            var stateMachine = gameManager.GetStateMachine();
            if (stateMachine != null && stateMachine.CurrentVehicle != null 
                && stateMachine.CurrentVehicle.controlType == ControlType.Player)
            {
                playerVehicle = stateMachine.CurrentVehicle;
            }
            else if (playerVehicle == null || playerVehicle.Status == VehicleStatus.Destroyed)
            {
                var playerVehicles = gameManager.GetPlayerVehicles();
                playerVehicle = playerVehicles.Find(v => v.Status != VehicleStatus.Destroyed);
            }
        }
        
        if (playerVehicle != null)
        {
            UpdatePlayerStatus();
            UpdateSameStageVehicles();
        }
        
        UpdateRecentEvents();
    }
    
    private void UpdatePlayerStatus()
    {
        if (playerStatusText == null || playerVehicle == null) return;
        
        string status = $"<b><size=18><color=#FFD700>PLAYER VEHICLE</color></size></b>\n\n";
        status += $"<b>Name:</b> {playerVehicle.vehicleName}\n";
        status += $"<b>Status:</b> {GetStatusColor(playerVehicle.Status)}\n\n";

        var playerStage = RacePositionTracker.GetStage(playerVehicle);
        if (playerStage != null)
        {
            status += $"<b>Stage:</b> {playerStage.stageName}\n";
            status += $"<b>Progress:</b> {RacePositionTracker.GetProgress(playerVehicle):F1}/{playerStage.length:F0}m\n\n";
        }
        
        int health = playerVehicle.Chassis?.GetCurrentHealth() ?? 0;
        int maxHealth = playerVehicle.Chassis?.GetMaxHealth() ?? 0;
        float healthPercent = maxHealth > 0 ? (float)health / maxHealth : 0f;
        string healthBar = UITextHelpers.GenerateBar(healthPercent, 15, brackets: true);
        string healthColor = UITextHelpers.GetHealthColor(healthPercent);
        status += $"<b>Health:</b> <color={healthColor}>{healthBar} {health}/{maxHealth}</color>\n";

        int energy = playerVehicle.PowerCore?.GetCurrentEnergy() ?? 0;
        int maxEnergy = playerVehicle.PowerCore?.GetMaxEnergy() ?? 0;
        float energyPercent = maxEnergy > 0 ? (float)energy / maxEnergy : 0f;
        string energyBar = UITextHelpers.GenerateBar(energyPercent, 15, brackets: true);
        status += $"<b>Energy:</b> <color=#88DDFF>{energyBar} {energy}/{maxEnergy}</color>\n\n";

        float speed = playerVehicle.Drive?.GetMaxSpeed() ?? 0f;
        int armorClass = playerVehicle.Chassis?.GetArmorClass() ?? 10;
        status += $"<b>Speed:</b> {speed:F1}\n";
        status += $"<b>Armor Class:</b> {armorClass}\n\n";

        var allStatusEffects = new List<AppliedEntityCondition>();
        foreach (var component in playerVehicle.AllComponents)
        {
            allStatusEffects.AddRange(component.GetActiveConditions());
        }
        
        if (allStatusEffects.Count > 0)
        {
            status += $"<b>Active Status Effects ({allStatusEffects.Count}):</b>\n";
            foreach (var applied in allStatusEffects)
            {
                string duration = applied.IsIndefinite ? "inf" : $"{applied.turnsRemaining}t";
                status += $"  - {applied.template.effectName} ({duration})\n";
            }
        }
        else
        {
            status += "<color=#888888>No active modifiers</color>\n";
        }
        
        playerStatusText.text = status;
    }
    
    private void UpdateSameStageVehicles()
    {
        if (sameStageVehiclesText == null || playerVehicle == null || RacePositionTracker.GetStage(playerVehicle) == null)
        {
            if (sameStageVehiclesText != null)
                sameStageVehiclesText.text = "<color=#888888>No other vehicles in this stage</color>";
            return;
        }
        
        if (gameManager == null)
        {
            sameStageVehiclesText.text = "<color=#888888>GameManager not found</color>";
            return;
        }
        
        var turnController = gameManager.GetTurnController();
        if (turnController == null)
        {
            sameStageVehiclesText.text = "<color=#888888>TurnController not found</color>";
            return;
        }
        
        var playerStage = RacePositionTracker.GetStage(playerVehicle);
        var sameStageVehicles = turnController.AllVehicles
            .Where(v => v != null && 
                        v != playerVehicle && 
                        v.Status == VehicleStatus.Active &&
                        RacePositionTracker.GetStage(v) == playerStage)
            .ToList();
        
        if (sameStageVehicles.Count == 0)
        {
            sameStageVehiclesText.text = "<color=#888888>No other vehicles in this stage</color>";
            return;
        }
        
        string display = $"<b><size=16><color=#FF8844>VEHICLES IN {playerStage.stageName.ToUpper()}</color></size></b>\n\n";
        display += $"<color=#FFAA44> {sameStageVehicles.Count} vehicle(s) in combat range!</color>\n\n";
        
        foreach (var vehicle in sameStageVehicles)
        {
            display += $"<b>{vehicle.vehicleName}</b> ({vehicle.controlType})\n";

            int health = vehicle.Chassis?.GetCurrentHealth() ?? 0;
            int maxHealth = vehicle.Chassis?.GetMaxHealth() ?? 0;
            float healthPercent = maxHealth > 0 ? (float)health / maxHealth : 0f;
            string healthBar = UITextHelpers.GenerateBar(healthPercent, 10, brackets: true);
            string healthColor = UITextHelpers.GetHealthColor(healthPercent);
            display += $"  HP: <color={healthColor}>{healthBar} {health}/{maxHealth}</color>\n";

            int energy = vehicle.PowerCore?.GetCurrentEnergy() ?? 0;
            int maxEnergy = vehicle.PowerCore?.GetMaxEnergy() ?? 0;
            display += $"  Energy: {energy}/{maxEnergy}\n";

            int armorClass = vehicle.Chassis?.GetArmorClass() ?? 10;
            float speed = vehicle.Drive?.GetMaxSpeed() ?? 0f;
            display += $"  AC: {armorClass} | ";
            display += $"Speed: {speed:F1}\n";
            
            display += $"  Progress: {RacePositionTracker.GetProgress(vehicle):F1}/{playerStage.length:F0}m\n";

            display += "\n";
        }

        sameStageVehiclesText.text = display;
    }

    private void UpdateRecentEvents()
    {
        if (recentEventsText == null) return;

        var recentEvents = RaceHistory.AllEvents
            .Where(e => e.importance <= EventImportance.High)
            .TakeLast(recentEventCount)
            .ToList();
        
        if (recentEvents.Count == 0)
        {
            recentEventsText.text = "<color=#888888>No recent events</color>";
            return;
        }
        
        string display = "<b><size=16>RECENT EVENTS</size></b>\n\n";
        
        foreach (var evt in recentEvents)
        {
            display += evt.GetFormattedText(includeTimestamp: true, includeLocation: false) + "\n";
        }
        
        recentEventsText.text = display;
    }
    
    private string GetStatusColor(VehicleStatus status)
    {
        return status switch
        {
            VehicleStatus.Active => "<color=#44FF44>Active</color>",
            VehicleStatus.Destroyed => "<color=#FF4444>Destroyed</color>",
            _ => status.ToString()
        };
    }
    
    }