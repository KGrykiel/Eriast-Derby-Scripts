using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Logging;
using Assets.Scripts.StatusEffects;

/// <summary>
/// Focus panel showing player status and same-stage vehicles.
/// Updates in real-time to show combat-relevant information.
/// </summary>
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
        
        // Get first player vehicle (or current one if multiple exist)
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
        // Update player vehicle reference in case it changed (e.g., current turn vehicle)
        if (gameManager != null)
        {
            var stateMachine = gameManager.GetStateMachine();
            if (stateMachine != null && stateMachine.CurrentVehicle != null 
                && stateMachine.CurrentVehicle.controlType == ControlType.Player)
            {
                // Show the currently active player vehicle
                playerVehicle = stateMachine.CurrentVehicle;
            }
            else if (playerVehicle == null || playerVehicle.Status == VehicleStatus.Destroyed)
            {
                // Fallback to first alive player vehicle
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
        
        // Current Stage
        if (playerVehicle.currentStage != null)
        {
            status += $"<b>Stage:</b> {playerVehicle.currentStage.stageName}\n";
            status += $"<b>Progress:</b> {playerVehicle.progress:F1}/{playerVehicle.currentStage.length:F0}m\n\n";
        }
        
        // Health (from chassis)
        int health = playerVehicle.chassis?.GetCurrentHealth() ?? 0;
        int maxHealth = playerVehicle.chassis?.GetMaxHealth() ?? 0;
        float healthPercent = maxHealth > 0 ? (float)health / maxHealth : 0f;
        string healthBar = GenerateBar(healthPercent, 15);
        string healthColor = GetHealthColor(healthPercent);
        status += $"<b>Health:</b> <color={healthColor}>{healthBar} {health}/{maxHealth}</color>\n";
        
        // Energy (from power core)
        int energy = playerVehicle.powerCore?.GetCurrentEnergy() ?? 0;
        int maxEnergy = playerVehicle.powerCore?.GetMaxEnergy() ?? 0;
        float energyPercent = maxEnergy > 0 ? (float)energy / maxEnergy : 0f;
        string energyBar = GenerateBar(energyPercent, 15);
        status += $"<b>Energy:</b> <color=#88DDFF>{energyBar} {energy}/{maxEnergy}</color>\n\n";
        
        // Attributes (from components)
        float speed = playerVehicle.GetDriveComponent()?.GetMaxSpeed() ?? 0f;
        int armorClass = playerVehicle.chassis?.GetArmorClass() ?? 10;
        status += $"<b>Speed:</b> {speed:F1}\n";
        status += $"<b>Armor Class:</b> {armorClass}\n";
        status += $"<b>Magic Resistance:</b> 10 \n\n"; // TODO: Replace with actual MR attribute

        // Active Status Effects across all components
        var allStatusEffects = new List<AppliedStatusEffect>();
        foreach (var component in playerVehicle.AllComponents)
        {
            allStatusEffects.AddRange(component.GetActiveStatusEffects());
        }
        
        if (allStatusEffects.Count > 0)
        {
            status += $"<b>Active Status Effects ({allStatusEffects.Count}):</b>\n";
            foreach (var applied in allStatusEffects)
            {
                string duration = applied.IsIndefinite ? "∞" : $"{applied.turnsRemaining}t";
                status += $"  • {applied.template.effectName} ({duration})\n";
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
        if (sameStageVehiclesText == null || playerVehicle == null || playerVehicle.currentStage == null)
        {
            if (sameStageVehiclesText != null)
                sameStageVehiclesText.text = "<color=#888888>No other vehicles in this stage</color>";
            return;
        }
        
        // Get all vehicles via GameManager
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
        
        // Get all active vehicles in the same stage (excluding player)
        var sameStageVehicles = turnController.AllVehicles
            .Where(v => v != null && 
                        v != playerVehicle && 
                        v.Status == VehicleStatus.Active &&
                        v.currentStage == playerVehicle.currentStage)
            .ToList();
        
        if (sameStageVehicles.Count == 0)
        {
            sameStageVehiclesText.text = "<color=#888888>No other vehicles in this stage</color>";
            return;
        }
        
        string display = $"<b><size=16><color=#FF8844>VEHICLES IN {playerVehicle.currentStage.stageName.ToUpper()}</color></size></b>\n\n";
        display += $"<color=#FFAA44> {sameStageVehicles.Count} vehicle(s) in combat range!</color>\n\n";
        
        foreach (var vehicle in sameStageVehicles)
        {
            display += $"<b>{vehicle.vehicleName}</b> ({vehicle.controlType})\n";
            
            // Health (from chassis)
            int health = vehicle.chassis?.GetCurrentHealth() ?? 0;
            int maxHealth = vehicle.chassis?.GetMaxHealth() ?? 0;
            float healthPercent = maxHealth > 0 ? (float)health / maxHealth : 0f;
            string healthBar = GenerateBar(healthPercent, 10);
            string healthColor = GetHealthColor(healthPercent);
            display += $"  HP: <color={healthColor}>{healthBar} {health}/{maxHealth}</color>\n";

            // Energy (from power core)
            int energy = vehicle.powerCore?.GetCurrentEnergy() ?? 0;
            int maxEnergy = vehicle.powerCore?.GetMaxEnergy() ?? 0;
            display += $"  Energy: {energy}/{maxEnergy}\n";
            
            // Key stats (from components)
            int armorClass = vehicle.chassis?.GetArmorClass() ?? 10;
            float speed = vehicle.GetDriveComponent()?.GetMaxSpeed() ?? 0f;
            display += $"  AC: {armorClass} | ";
            display += $"Speed: {speed:F1}\n";
            
            // Progress
            display += $"  Progress: {vehicle.progress:F1}/{vehicle.currentStage.length:F0}m\n";
            
            display += "\n";
        }
        
        sameStageVehiclesText.text = display;
    }
    
    private void UpdateRecentEvents()
    {
        if (recentEventsText == null) return;
        
        // Get recent high-importance events
        var recentEvents = RaceHistory.Instance.AllEvents
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
    
    // Helper methods
    
    private string GetStatusColor(VehicleStatus status)
    {
        return status switch
        {
            VehicleStatus.Active => "<color=#44FF44>Active</color>",
            VehicleStatus.Destroyed => "<color=#FF4444>Destroyed</color>",
            _ => status.ToString()
        };
    }
    
    private string GetHealthColor(float percent)
    {
        if (percent > 0.6f) return "#44FF44";
        if (percent > 0.3f) return "#FFFF44";
        return "#FF4444";
    }
    
    private string GenerateBar(float percent, int length)
    {
        int filled = Mathf.RoundToInt(percent * length);
        filled = Mathf.Clamp(filled, 0, length);
        
        string bar = "[";
        for (int i = 0; i < length; i++)
        {
            bar += i < filled ? "#" : "-";
        }
        bar += "]";
        
        return bar;
    }
}