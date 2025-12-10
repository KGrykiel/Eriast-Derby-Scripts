using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using RacingGame.Events;
using Assets.Scripts.VehicleComponents;

/// <summary>
/// Inspector panel for detailed vehicle examination.
/// Shows full stats, modifiers, skills, and event history.
/// Refreshes on turn changes and vehicle selection.
/// </summary>
public class VehicleInspectorPanel : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Dropdown vehicleDropdown;
    public TextMeshProUGUI detailText;
    public Button refreshButton;
    
    private List<Vehicle> allVehicles = new List<Vehicle>();
    private Vehicle selectedVehicle;
    private bool initialized = false;
    
    void Start()
    {
        if (refreshButton != null)
        {
            refreshButton.onClick.AddListener(RefreshDetails);
        }
        
   // Try to initialize, but don't fail if GameManager isn't ready yet
        TryInitialize();
    }

    void Update()
    {
        // Keep trying to initialize until successful
    if (!initialized)
      {
            TryInitialize();
  }
    }

    private void TryInitialize()
    {
 var gameManager = FindObjectOfType<GameManager>();
  if (gameManager != null)
        {
          var vehicles = gameManager.GetVehicles();
            
   // Check if vehicles list is valid
       if (vehicles != null && vehicles.Count > 0)
    {
       allVehicles = vehicles;
     PopulateDropdown();
       initialized = true;
    }
        }
    }
    
    /// <summary>
    /// Called externally (e.g., by GameManager) when turn state changes.
    /// Public method so other systems can trigger refresh.
/// </summary>
    public void OnTurnChanged()
    {
      if (initialized && gameObject.activeInHierarchy)
        {
     RefreshDetails();
        }
    }
    
    private void PopulateDropdown()
{
        if (vehicleDropdown == null) return;
  
        // Safety check: ensure we have vehicles
        if (allVehicles == null || allVehicles.Count == 0)
        {
  Debug.LogWarning("[VehicleInspectorPanel] No vehicles available to display");
            return;
        }
 
        vehicleDropdown.ClearOptions();
    
        // Add listener first (before setting options)
        vehicleDropdown.onValueChanged.RemoveAllListeners();
        vehicleDropdown.onValueChanged.AddListener(OnVehicleSelected);
  
        List<string> options = new List<string>();
   foreach (var vehicle in allVehicles)
        {
            if (vehicle == null) continue; // Skip null vehicles
    
    string statusIcon = vehicle.Status == VehicleStatus.Active ? "[OK]" : "[X]";
   string typeIcon = vehicle.controlType == ControlType.Player ? "(P)" : "(AI)";
        options.Add($"{statusIcon} {typeIcon} {vehicle.vehicleName}");
        }
      
    vehicleDropdown.AddOptions(options);
        
     if (allVehicles.Count > 0)
        {
      selectedVehicle = allVehicles[0];
      RefreshDetails();
        }
    }
    
    private void OnVehicleSelected(int index)
    {
 if (index >= 0 && index < allVehicles.Count && allVehicles[index] != null)
        {
  selectedVehicle = allVehicles[index];
      RefreshDetails();
}
    }
 
    public void RefreshDetails()
    {
        if (detailText == null || selectedVehicle == null) return;
    
string display = BuildDetailedVehicleInfo();
      detailText.text = display;
    }
    
    private string BuildDetailedVehicleInfo()
    {
        if (selectedVehicle == null)
        {
            return "<color=#888888>No vehicle selected</color>";
        }
        
        string info = $"<b><size=20>{selectedVehicle.vehicleName}</size></b>\n";
        info += $"<color=#888888>========================</color>\n\n";
        
        // Basic Info
        info += "<b>BASIC INFO:</b>\n";
        info += $"  Control: {selectedVehicle.controlType}\n";
        info += $"  Status: {GetStatusColor(selectedVehicle.Status)}\n";
        
        if (selectedVehicle.currentStage != null)
        {
            info += $"  Stage: {selectedVehicle.currentStage.stageName}\n";
            info += $"  Progress: {selectedVehicle.progress:F1}/{selectedVehicle.currentStage.length:F0}m\n";
        }
        
        info += "\n";
        
        // Stats
        info += "<b>STATS:</b>\n";
        float maxHealth = selectedVehicle.GetAttribute(Attribute.MaxHealth);
        float healthPercent = selectedVehicle.health / maxHealth;
        string healthBar = GenerateBar(healthPercent, 15);
        string healthColor = GetHealthColor(healthPercent);
        info += $"  HP: <color={healthColor}>{healthBar} {selectedVehicle.health:F0}/{maxHealth:F0}</color>\n";
        
        int maxEnergy = (int)selectedVehicle.GetAttribute(Attribute.MaxEnergy);
        float energyPercent = (float)selectedVehicle.energy / maxEnergy;
        string energyBar = GenerateBar(energyPercent, 15);
        info += $"  Energy: <color=#88DDFF>{energyBar} {selectedVehicle.energy}/{maxEnergy}</color>\n";
        
        info += $"  Speed: {selectedVehicle.GetAttribute(Attribute.Speed):F1}\n";
        info += $"  AC: {selectedVehicle.GetAttribute(Attribute.ArmorClass):F0}\n";
        info += $"  Magic Resist: {selectedVehicle.GetAttribute(Attribute.MagicResistance):F0}\n";
        info += $"  Energy Regen: {selectedVehicle.GetAttribute(Attribute.EnergyRegen):F1}\n";
        
        info += "\n";
        
        // Components
        info += "<b>COMPONENTS:</b>\n";
        info += BuildComponentsInfo();
        
        info += "\n";
        
        // Active Modifiers
        var modifiers = selectedVehicle.GetActiveModifiers();
        info += $"<b>ACTIVE MODIFIERS ({modifiers.Count}):</b>\n";
        
        if (modifiers.Count == 0)
        {
            info += "  <color=#888888>None</color>\n";
        }
        else
        {
            foreach (var mod in modifiers)
            {
                string durText = mod.DurationTurns > 0 ? $" ({mod.DurationTurns} turns)" : " (permanent)";
                string sourceText = mod.Source != null ? $" [from {mod.Source.name}]" : "";
                info += $"  - {mod.Type} {mod.Attribute} {mod.Value:+0;-0}{durText}{sourceText}\n";
            }
        }
        
        info += "\n";
        
        // Skills
        info += $"<b>SKILLS ({selectedVehicle.skills.Count}):</b>\n";
        
        if (selectedVehicle.skills.Count == 0)
        {
            info += "  <color=#888888>None</color>\n";
        }
        else
        {
            foreach (var skill in selectedVehicle.skills)
            {
                if (skill != null)
                {
                    bool canAfford = selectedVehicle.energy >= skill.energyCost;
                    string affordText = canAfford ? "" : " <color=#FF4444>(Can't afford)</color>";
                    info += $"  - <b>{skill.name}</b> ([POWER]{skill.energyCost}){affordText}\n";
                    
                    if (!string.IsNullOrEmpty(skill.description))
                    {
                        info += $"    <color=#AAAAAA>{skill.description}</color>\n";
                    }
                }
            }
        }
        
        info += "\n";
        
        // Event History
        var vehicleEvents = RaceHistory.GetVehicleEvents(selectedVehicle);
        info += $"<b>EVENT HISTORY ({vehicleEvents.Count} events):</b>\n";
        
        if (vehicleEvents.Count == 0)
        {
            info += "  <color=#888888>No events recorded</color>\n";
        }
        else
        {
            // Show last 10 events
            var recentEvents = vehicleEvents.TakeLast(10).ToList();
            foreach (var evt in recentEvents)
            {
                info += $"  {evt.GetFormattedText(includeTimestamp: true, includeLocation: false)}\n";
            }
            
            if (vehicleEvents.Count > 10)
            {
                info += $"  <color=#888888>... and {vehicleEvents.Count - 10} more</color>\n";
            }
        }
        
        info += "\n";
        
        // Story Summary
        info += "<b>NARRATIVE SUMMARY:</b>\n";
        string story = RaceHistory.GenerateVehicleStory(selectedVehicle);
        info += story;
        
        return info;
    }
    
    // Helper methods
    
    /// <summary>
    /// Builds detailed component information with health, AC, and accessibility.
    /// Color-coded by health percentage: Green > 60%, Yellow 30-60%, Red < 30%.
    /// </summary>
    private string BuildComponentsInfo()
    {
        if (selectedVehicle == null)
            return "  <color=#888888>No vehicle data</color>\n";
        
        var allComponents = selectedVehicle.AllComponents;
        
        if (allComponents == null || allComponents.Count == 0)
        {
            return "  <color=#888888>No components installed</color>\n";
        }
        
        string componentInfo = "";
        
        foreach (var component in allComponents)
        {
            if (component == null) continue;
            
            // Component icon based on type
            string icon = GetComponentIcon(component);
            
            // Component name from GameObject
            string componentName = component.name;
            
            // HP bar and color using Entity fields
            float hpPercent = component.maxHealth > 0 
                ? (float)component.health / component.maxHealth 
                : 0f;
            string hpColor = GetHealthColor(hpPercent);
            string hpBar = GenerateBar(hpPercent, 10);
            string hpText = $"{component.health}/{component.maxHealth}";
            
            // Destroyed status
            string statusText = "";
            if (component.isDestroyed)
            {
                statusText = " <color=#FF4444>DESTROYED</color>";
                hpColor = "#888888";
            }
            else if (component.isDisabled)
            {
                statusText = " <color=#FFAA44>DISABLED</color>";
            }
            
            // Component line with HP bar
            componentInfo += $"  {icon} <b>{componentName}</b> <color={hpColor}>{hpBar} {hpText}</color>{statusText}\n";
            
            // Component details (AC, exposure, role) using Entity.armorClass
            componentInfo += $"    AC: {component.armorClass}";
            
            // Exposure/Accessibility
            string exposureInfo = GetExposureInfo(component);
            if (!string.IsNullOrEmpty(exposureInfo))
            {
                componentInfo += $" | {exposureInfo}";
            }
            
            // Role info
            if (component.enablesRole)
            {
                string roleName = component.roleName;
                string characterName = component.assignedCharacter?.characterName ?? "Unassigned";
                componentInfo += $" | Role: <color=#AADDFF>{roleName}</color> ({characterName})";
            }
            
            componentInfo += "\n";
            
            // Show inaccessibility reason if applicable
            if (!component.isDestroyed && !selectedVehicle.IsComponentAccessible(component))
            {
                string reason = selectedVehicle.GetInaccessibilityReason(component);
                componentInfo += $"    <color=#FFAA44>! {reason}</color>\n";
            }
        }
        
        return componentInfo;
    }
    
    /// <summary>
    /// Gets an icon/emoji for a component based on its type and state.
    /// </summary>
    private string GetComponentIcon(VehicleComponent component)
    {
        if (component.isDestroyed)
            return "[X]";
        
        // Icon based on component type
        if (component is ChassisComponent)
            return "[#]"; // Shield/armor
        if (component is PowerCoreComponent)
            return "[*]"; // Power/energy
        if (component is DriveComponent)
            return "[>]"; // Movement
        if (component is WeaponComponent)
            return "[!]"; // Weapon/attack
        
        // Generic component types
        switch (component.componentType)
        {
            case ComponentType.ActiveDefense:
                return "[#]";
            case ComponentType.Sensors:
                return "[?]";
            case ComponentType.Communications:
                return "[~]";
            case ComponentType.Utility:
                return "[+]";
            default:
                return "[o]";
        }
    }
    
    /// <summary>
    /// Gets exposure/accessibility information for a component.
    /// </summary>
    private string GetExposureInfo(VehicleComponent component)
    {
        string exposureText = "";
        
        switch (component.exposure)
        {
            case ComponentExposure.External:
                exposureText = "<color=#44FF44>External</color>";
                break;
            case ComponentExposure.Protected:
                exposureText = "<color=#FFAA44>Protected</color>";
                if (!string.IsNullOrEmpty(component.shieldedBy))
                {
                    exposureText += $" (by {component.shieldedBy})";
                }
                break;
            case ComponentExposure.Internal:
                exposureText = "<color=#FF8844>Internal</color>";
                int threshold = Mathf.RoundToInt(component.internalAccessThreshold * 100);
                exposureText += $" ({threshold}% dmg)";
                break;
            case ComponentExposure.Shielded:
                exposureText = "<color=#88DDFF>Shielded</color>";
                if (!string.IsNullOrEmpty(component.shieldedBy))
                {
                    exposureText += $" (by {component.shieldedBy})";
                }
                break;
        }
        
        return exposureText;
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
            bar += i < filled ? "#" : "-"; // Changed from █ and ░
        }
        bar += "]";
        
        return bar;
    }
}