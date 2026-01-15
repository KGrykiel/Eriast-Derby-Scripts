using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using RacingGame.Events;

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
        
        if (gameManager != null)
        {
            playerVehicle = gameManager.playerVehicle;
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
        
        // Health
        float maxHealth = playerVehicle.maxHealth;
        float healthPercent = maxHealth > 0 ? playerVehicle.health / maxHealth : 0f;
        string healthBar = GenerateBar(healthPercent, 15);
        string healthColor = GetHealthColor(healthPercent);
        status += $"<b>Health:</b> <color={healthColor}>{healthBar} {playerVehicle.health:F0}/{maxHealth:F0}</color>\n";
        
        // Energy
        int maxEnergy = (int)playerVehicle.maxEnergy;
        float energyPercent = maxEnergy > 0 ? (float)playerVehicle.energy / maxEnergy : 0f;
        string energyBar = GenerateBar(energyPercent, 15);
        status += $"<b>Energy:</b> <color=#88DDFF>{energyBar} {playerVehicle.energy}/{maxEnergy}</color>\n\n";
        
        // Attributes
        status += $"<b>Speed:</b> {playerVehicle.speed:F1}\n";
        status += $"<b>Armor Class:</b> {playerVehicle.armorClass:F0}\n";
        status += $"<b>Magic Resistance:</b> 10 \n\n"; // TODO: Replace with actual MR attribute

        // Active Status Effects across all components
        var allStatusEffects = new List<StatusEffects.AppliedStatusEffect>();
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
        
        // Get all vehicles in the same stage using TurnController
        var turnController = FindFirstObjectByType<TurnController>();
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
        display += $"<color=#FFAA44>⚔ {sameStageVehicles.Count} vehicle(s) in combat range!</color>\n\n";
        
        foreach (var vehicle in sameStageVehicles)
        {
            display += $"<b>{vehicle.vehicleName}</b> ({vehicle.controlType})\n";
            
            // Health
            float maxHealth = vehicle.maxHealth;
            float healthPercent = maxHealth > 0 ? vehicle.health / maxHealth : 0f;
            string healthBar = GenerateBar(healthPercent, 10);
            string healthColor = GetHealthColor(healthPercent);
            display += $"  HP: <color={healthColor}>{healthBar} {vehicle.health:F0}/{maxHealth:F0}</color>\n";

            // Energy
            int maxEnergy = (int)vehicle.maxEnergy;
            display += $"  Energy: {vehicle.energy}/{maxEnergy}\n";
            
            // Key stats
            display += $"  AC: {vehicle.armorClass:F0} | ";
            display += $"Speed: {vehicle.speed:F1}\n";
            
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