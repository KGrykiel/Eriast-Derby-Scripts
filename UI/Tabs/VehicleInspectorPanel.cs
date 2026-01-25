using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Logging;
using Assets.Scripts.UI.Components;
using Assets.Scripts.Core;

/// <summary>
/// Inspector panel for detailed vehicle examination.
/// Shows full stats, modifiers, skills, and event history.
/// Refreshes on turn changes and vehicle selection.
/// 
/// NEW: Uses structured UI with StatValueDisplay components and dynamic ComponentEntry prefabs.
/// </summary>
public class VehicleInspectorPanel : MonoBehaviour
{
    [Header("Header")]
    public TMP_Dropdown vehicleDropdown;
    public Button refreshButton;
    
    [Header("Basic Info Section")]
    public TMP_Text vehicleNameText;
    public TMP_Text controlValueText;
    public TMP_Text statusValueText;
    public TMP_Text stageValueText;
    public Slider progressBar;
    public TMP_Text progressText;
    
    [Header("Stats Section")]
    public TMP_Text vehicleHPValueText;
    public Slider vehicleHPBar;
    public TMP_Text vehicleEnergyValueText;
    public Slider vehicleEnergyBar;
    public TMP_Text vehicleSpeedValueText;
    public TMP_Text vehicleACValueText;
    public TMP_Text vehicleEnergyRegenValueText;
    
    [Header("Components Section")]
    public Transform componentListContainer;
    public GameObject componentEntryPrefab;
    
    [Header("Dynamic Stat Field")]
    [Tooltip("Prefab for dynamically created stat fields (must have ComponentStatField component)")]
    public GameObject statFieldPrefab;
    
    [Header("Legacy Text Sections")]
    public TMP_Text skillsSectionText;
    public TMP_Text eventHistorySectionText;
    
    // Private state
    private List<Vehicle> allVehicles = new List<Vehicle>();
    private Vehicle selectedVehicle;
    private bool initialized = false;
    
    // Component entry pool
    private List<GameObject> componentEntryInstances = new List<GameObject>();
    
    // Dirty tracking for refresh
    private int lastEventCount = 0;
    
    void Start()
    {
        if (refreshButton != null)
        {
            refreshButton.onClick.AddListener(RefreshDetails);
        }
        
        TryInitialize();
    }
    
    void OnEnable()
    {
        // Force refresh when panel becomes active
        if (initialized)
        {
            RefreshDetails();
        }
    }

    void Update()
    {
        // Try to initialize if not ready
        if (!initialized)
        {
            TryInitialize();
            return;
        }
        
        // Simple polling: refresh when event count changes
        if (RaceHistory.Instance != null)
        {
            int currentEventCount = RaceHistory.Instance.AllEvents.Count;
            if (currentEventCount != lastEventCount)
            {
                lastEventCount = currentEventCount;
                RefreshDetails();
            }
        }
    }

    private void TryInitialize()
    {
        var gameManager = FindFirstObjectByType<GameManager>();
        if (gameManager != null)
        {
            var vehicles = gameManager.GetVehicles();
            
            if (vehicles != null && vehicles.Count > 0)
            {
                allVehicles = vehicles;
                PopulateDropdown();
                initialized = true;
                
                // Initialize event count
                if (RaceHistory.Instance != null)
                {
                    lastEventCount = RaceHistory.Instance.AllEvents.Count;
                }
            }
        }
    }
    
    /// <summary>
    /// Called externally when turn state changes.
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
  
        if (allVehicles == null || allVehicles.Count == 0)
        {
            Debug.LogWarning("[VehicleInspectorPanel] No vehicles available to display");
            return;
        }
 
        vehicleDropdown.ClearOptions();
        vehicleDropdown.onValueChanged.RemoveAllListeners();
        vehicleDropdown.onValueChanged.AddListener(OnVehicleSelected);
  
        List<string> options = new List<string>();
        foreach (var vehicle in allVehicles)
        {
            if (vehicle == null) continue;
    
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
        if (selectedVehicle == null) return;
        
        PopulateBasicInfo();
        PopulateStats();
        PopulateComponents();
        PopulateSkills(); // Legacy text-based
        PopulateEventHistory(); // Legacy text-based
    }
    
    // ==================== BASIC INFO SECTION ====================
    
    private void PopulateBasicInfo()
    {
        if (vehicleNameText != null)
        {
            vehicleNameText.text = selectedVehicle.vehicleName;
        }
        
        if (controlValueText != null)
        {
            controlValueText.text = selectedVehicle.controlType.ToString();
        }
        
        if (statusValueText != null)
        {
            statusValueText.text = GetStatusString(selectedVehicle.Status);
            statusValueText.color = GetStatusColor(selectedVehicle.Status);
        }
        
        if (stageValueText != null)
        {
            if (selectedVehicle.currentStage != null)
            {
                stageValueText.text = selectedVehicle.currentStage.stageName;
            }
            else
            {
                stageValueText.text = "N/A";
            }
        }
        
        if (progressBar != null && progressText != null && selectedVehicle.currentStage != null)
        {
            float progress = selectedVehicle.progress;
            float length = selectedVehicle.currentStage.length;
            float percent = length > 0 ? progress / length : 0f;
            
            progressBar.value = percent;
            progressText.text = $"{progress:F1}/{length:F0}m";
        }
    }
    
    // ==================== STATS SECTION ====================
    
    private void PopulateStats()
    {
        // HP
        if (vehicleHPValueText != null)
        {
            var hpDisplay = vehicleHPValueText.GetComponent<StatValueDisplay>();
            if (hpDisplay != null)
            {
                // Use StatCalculator to get modified max HP
                float baseMaxHP = selectedVehicle.chassis != null ? selectedVehicle.chassis.maxHealth : 100;
                float modifiedMaxHP = selectedVehicle.maxHealth;
                
                hpDisplay.UpdateDisplay(
                    selectedVehicle.chassis,
                    Attribute.MaxHealth,
                    baseMaxHP,
                    modifiedMaxHP,
                    $"{selectedVehicle.health}/{modifiedMaxHP}"
                );
            }
            else
            {
                vehicleHPValueText.text = $"{selectedVehicle.health}/{selectedVehicle.maxHealth}";
            }
            
            if (vehicleHPBar != null)
            {
                float percent = selectedVehicle.maxHealth > 0 
                    ? (float)selectedVehicle.health / selectedVehicle.maxHealth 
                    : 0f;
                vehicleHPBar.value = percent;
            }
        }
        
        // Energy
        if (vehicleEnergyValueText != null)
        {
            var energyDisplay = vehicleEnergyValueText.GetComponent<StatValueDisplay>();
            if (energyDisplay != null)
            {
                float baseMaxEnergy = selectedVehicle.powerCore != null ? selectedVehicle.powerCore.maxEnergy : 100;
                float modifiedMaxEnergy = selectedVehicle.maxEnergy;
                
                energyDisplay.UpdateDisplay(
                    selectedVehicle.powerCore,
                    Attribute.MaxEnergy,
                    baseMaxEnergy,
                    modifiedMaxEnergy,
                    $"{selectedVehicle.energy}/{modifiedMaxEnergy}"
                );
            }
            else
            {
                vehicleEnergyValueText.text = $"{selectedVehicle.energy}/{selectedVehicle.maxEnergy}";
            }
            
            if (vehicleEnergyBar != null)
            {
                float percent = selectedVehicle.maxEnergy > 0 
                    ? (float)selectedVehicle.energy / selectedVehicle.maxEnergy 
                    : 0f;
                vehicleEnergyBar.value = percent;
            }
        }
        
        // Speed
        if (vehicleSpeedValueText != null)
        {
            var speedDisplay = vehicleSpeedValueText.GetComponent<StatValueDisplay>();
            if (speedDisplay != null && selectedVehicle.optionalComponents != null)
            {
                var drive = selectedVehicle.optionalComponents.OfType<DriveComponent>().FirstOrDefault();
                if (drive != null)
                {
                    float baseSpeed = drive.maxSpeed;
                    float modifiedSpeed = selectedVehicle.speed;
                    
                    speedDisplay.UpdateDisplay(
                        drive,
                        Attribute.Speed,
                        baseSpeed,
                        modifiedSpeed,
                        $"{modifiedSpeed:F1}"
                    );
                }
                else
                {
                    // No drive component - show 0 with no modifiers (resets color to white)
                    speedDisplay.UpdateDisplaySimple(0, 0, "0");
                }
            }
            else
            {
                vehicleSpeedValueText.text = $"{selectedVehicle.speed:F1}";
            }
        }
        
        // AC
        if (vehicleACValueText != null)
        {
            var acDisplay = vehicleACValueText.GetComponent<StatValueDisplay>();
            if (acDisplay != null && selectedVehicle.chassis != null)
            {
                int baseAC = selectedVehicle.chassis.armorClass;
                int modifiedAC = selectedVehicle.armorClass;
                
                acDisplay.UpdateDisplay(
                    selectedVehicle.chassis,
                    Attribute.ArmorClass,
                    baseAC,
                    modifiedAC,
                    modifiedAC.ToString()
                );
            }
            else if (acDisplay != null)
            {
                // No chassis - show default AC with no modifiers (resets color to white)
                acDisplay.UpdateDisplaySimple(10, 10, selectedVehicle.armorClass.ToString());
            }
            else
            {
                vehicleACValueText.text = selectedVehicle.armorClass.ToString();
            }
        }
        
        // Energy Regen - with StatValueDisplay tooltip support
        if (vehicleEnergyRegenValueText != null)
        {
            var regenDisplay = vehicleEnergyRegenValueText.GetComponent<StatValueDisplay>();
            if (regenDisplay != null && selectedVehicle.powerCore != null)
            {
                float baseRegen = selectedVehicle.powerCore.energyRegen;
                float modifiedRegen = selectedVehicle.energyRegen;
                
                regenDisplay.UpdateDisplay(
                    selectedVehicle.powerCore,
                    Attribute.EnergyRegen,
                    baseRegen,
                    modifiedRegen,
                    $"{modifiedRegen:F1}"
                );
            }
            else if (regenDisplay != null)
            {
                // No power core - show 0 with no modifiers (resets color to white)
                regenDisplay.UpdateDisplaySimple(0, 0, $"{selectedVehicle.energyRegen:F1}");
            }
            else
            {
                vehicleEnergyRegenValueText.text = $"{selectedVehicle.energyRegen:F1}";
            }
        }
    }
    
    // ==================== COMPONENTS SECTION ====================
    
    private void PopulateComponents()
    {
        if (componentListContainer == null || componentEntryPrefab == null)
        {
            Debug.LogWarning("[VehicleInspectorPanel] Component list container or prefab not assigned!");
            return;
        }
        
        // Ensure ComponentListContainer has necessary components for dynamic sizing
        var contentSizeFitter = componentListContainer.GetComponent<ContentSizeFitter>();
        if (contentSizeFitter == null)
        {
            contentSizeFitter = componentListContainer.gameObject.AddComponent<ContentSizeFitter>();
            contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }
        
        var verticalLayout = componentListContainer.GetComponent<VerticalLayoutGroup>();
        if (verticalLayout == null)
        {
            verticalLayout = componentListContainer.gameObject.AddComponent<VerticalLayoutGroup>();
            verticalLayout.spacing = 12;
            verticalLayout.childForceExpandWidth = true;
            verticalLayout.childForceExpandHeight = false;
            verticalLayout.childControlWidth = true;
            verticalLayout.childControlHeight = true;
        }
        
        // Clear existing component entries
        foreach (var entry in componentEntryInstances)
        {
            if (entry != null)
            {
                Destroy(entry);
            }
        }
        componentEntryInstances.Clear();
        
        // Create new component entries
        var allComponents = selectedVehicle.AllComponents;
        if (allComponents == null || allComponents.Count == 0)
        {
            return;
        }
        
        foreach (var component in allComponents)
        {
            if (component == null) continue;
            
            GameObject entryObj = Instantiate(componentEntryPrefab, componentListContainer);
            componentEntryInstances.Add(entryObj);
            
            PopulateComponentEntry(entryObj, component);
        }
        
        // Force layout rebuild after creating all entries
        StartCoroutine(ForceLayoutRebuild());
    }
    
    /// <summary>
    /// Force Unity's layout system to recalculate all sizes.
    /// Needed because layout groups don't always update immediately when content changes.
    /// </summary>
    private System.Collections.IEnumerator ForceLayoutRebuild()
    {
        yield return null; // Wait one frame for instantiation to complete
        
        // Rebuild layout from bottom up
        if (componentListContainer != null)
        {
            var rectTransform = componentListContainer as RectTransform;
            LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
        }
        
        // Rebuild parent content container
        Transform content = componentListContainer?.parent;
        while (content != null)
        {
            var contentSizeFitter = content.GetComponent<ContentSizeFitter>();
            if (contentSizeFitter != null)
            {
                var rectTransform = content as RectTransform;
                LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
                break;
            }
            content = content.parent;
        }
    }
    
    private void PopulateComponentEntry(GameObject entryObj, VehicleComponent component)
    {
        // IMPORTANT: Ensure the entry has a Layout Element for proper sizing
        var layoutElement = entryObj.GetComponent<LayoutElement>();
        if (layoutElement == null)
        {
            layoutElement = entryObj.AddComponent<LayoutElement>();
        }
        layoutElement.minHeight = 80;
        layoutElement.preferredHeight = -1;
        
        // ==================== COMPONENT HEADER ====================
        var componentIcon = entryObj.transform.Find("ComponentHeader/ComponentIcon")?.GetComponent<Image>();
        var componentName = entryObj.transform.Find("ComponentHeader/ComponentName")?.GetComponent<TMP_Text>();
        var destroyedIcon = entryObj.transform.Find("ComponentHeader/StatusIconGroup/DestroyedIcon");
        var disabledIcon = entryObj.transform.Find("ComponentHeader/StatusIconGroup/DisabledIcon");
        
        if (componentName != null)
        {
            componentName.text = component.name;
        }
        
        // Leave component icon color as-is (don't override sprite colors)
        
        if (destroyedIcon != null)
        {
            destroyedIcon.gameObject.SetActive(component.isDestroyed);
        }
        
        if (disabledIcon != null)
        {
            // Show disabled icon if:
            // - Manually disabled (isDisabled = true), OR
            // - Cannot act due to status effects (stunned, etc.)
            // But not if destroyed (destroyed icon takes priority)
            bool showDisabled = !component.isDestroyed && !component.CanAct();
            disabledIcon.gameObject.SetActive(showDisabled);
        }
        
        // ==================== EXPOSURE (separate from stats) ====================
        var exposureValueText = entryObj.transform.Find("ExposureRow/ExposureValueText")?.GetComponent<TMP_Text>()
                             ?? entryObj.transform.Find("ComponentHeader/ExposureValueText")?.GetComponent<TMP_Text>()
                             ?? entryObj.transform.Find("ExposureValueText")?.GetComponent<TMP_Text>();
        
        if (exposureValueText != null)
        {
            exposureValueText.text = GetExposureString(component.exposure);
            exposureValueText.color = GetExposureColor(component.exposure);
        }
        
        // ==================== UNIVERSAL STATS (HP, AC) ====================
        
        var hpValueText = entryObj.transform.Find("ComponentStatsRow/HPGroup/HPValueText")?.GetComponent<TMP_Text>();
        var hpBar = entryObj.transform.Find("ComponentStatsRow/HPGroup/HPBar")?.GetComponent<Slider>();
        var acValueText = entryObj.transform.Find("ComponentStatsRow/ACGroup/ACValueText")?.GetComponent<TMP_Text>();
        
        // HP Text - with StatValueDisplay tooltip support
        if (hpValueText != null)
        {
            var hpDisplay = hpValueText.GetComponent<StatValueDisplay>();
            if (hpDisplay != null)
            {
                hpDisplay.UpdateDisplay(
                    component,
                    Attribute.MaxHealth,
                    component.maxHealth,
                    component.maxHealth,
                    $"{component.health}/{component.maxHealth}"
                );
            }
            else
            {
                hpValueText.text = $"{component.health}/{component.maxHealth}";
            }
        }
        
        // HP Bar
        if (hpBar != null)
        {
            float percent = component.maxHealth > 0 
                ? (float)component.health / (float)component.maxHealth 
                : 0f;
            hpBar.minValue = 0f;
            hpBar.maxValue = 1f;
            hpBar.value = percent;
        }
        
        // AC - with StatValueDisplay tooltip support
        if (acValueText != null)
        {
            int baseAC = component.armorClass;
            int modifiedAC = StatCalculator.GatherDefenseValue(component);
            
            var acDisplay = acValueText.GetComponent<StatValueDisplay>();
            if (acDisplay != null)
            {
                acDisplay.UpdateDisplay(
                    component,
                    Attribute.ArmorClass,
                    baseAC,
                    modifiedAC,
                    modifiedAC.ToString()
                );
            }
            else
            {
                acValueText.text = modifiedAC.ToString();
            }
        }
        
        // ==================== DYNAMIC STATS (inline, to the right of HP/AC) ====================
        // Look for DynamicStatsContainer inside ComponentStatsRow (same row, right side)
        
        var dynamicStatsContainer = entryObj.transform.Find("ComponentStatsRow/DynamicStatsContainer")
                                 ?? entryObj.transform.Find("ComponentStatsRow/DynamicStats")
                                 ?? entryObj.transform.Find("DynamicStatsRow");
        
        if (dynamicStatsContainer != null)
        {
            // Ensure container has HorizontalLayoutGroup for inline display
            ConfigureStatsContainerLayout(dynamicStatsContainer);
            
            // Clear any previously created dynamic stat fields
            ClearDynamicStatFields(dynamicStatsContainer);
            
            // Get display stats from component (each component type defines its own stats)
            var displayStats = component.GetDisplayStats();
            
            foreach (var stat in displayStats)
            {
                CreateStatField(dynamicStatsContainer, component, stat);
            }
        }
        
        // ==================== STATUS EFFECT BAR ====================
        var statusBar = entryObj.transform.Find("ComponentStatusEffectBar")?.GetComponent<StatusEffectBar>();
        if (statusBar != null)
        {
            statusBar.SetEntity(component);
            statusBar.Refresh();
        }
        
        // ==================== ROLE INFO ROW (with Inaccessibility Warning) ====================
        var roleInfoRow = entryObj.transform.Find("RoleInfoRow");
        if (roleInfoRow != null)
        {
            var roleName = roleInfoRow.Find("RoleName")?.GetComponent<TMP_Text>();
            var characterName = roleInfoRow.Find("CharacterName")?.GetComponent<TMP_Text>();
            var warningText = roleInfoRow.Find("InaccessibilityWarning")?.GetComponent<TMP_Text>();
            
            bool showRoleInfo = component.roleType != RoleType.None;
            bool isAccessible = selectedVehicle.IsComponentAccessible(component);
            bool showWarning = !component.isDestroyed && !isAccessible;
            
            roleInfoRow.gameObject.SetActive(showRoleInfo || showWarning);
            
            if (roleName != null)
            {
                roleName.gameObject.SetActive(showRoleInfo);
                if (showRoleInfo) roleName.text = component.roleType.ToString();
            }
            
            if (characterName != null)
            {
                characterName.gameObject.SetActive(showRoleInfo);
                if (showRoleInfo)
                {
                    // Get character from seat that controls this component
                    var seat = selectedVehicle.GetSeatForComponent(component);
                    var character = seat?.assignedCharacter;
                    characterName.text = character != null 
                        ? $"({character.characterName})" 
                        : "(Unassigned)";
                }
            }
            
            if (warningText != null)
            {
                warningText.gameObject.SetActive(showWarning);
                if (showWarning)
                {
                    string reason = selectedVehicle.GetInaccessibilityReason(component);
                    warningText.text = $"⚠ {reason}";
                }
            }
        }
    }
    
    /// <summary>
    /// Configure the stats container to use horizontal layout for inline stat display.
    /// </summary>
    private void ConfigureStatsContainerLayout(Transform container)
    {
        var layoutGroup = container.GetComponent<HorizontalLayoutGroup>();
        if (layoutGroup == null)
        {
            layoutGroup = container.gameObject.AddComponent<HorizontalLayoutGroup>();
        }
        
        layoutGroup.spacing = 8f;
        layoutGroup.childAlignment = TextAnchor.MiddleLeft;
        layoutGroup.childControlWidth = true;
        layoutGroup.childControlHeight = true;
        layoutGroup.childForceExpandWidth = false;
        layoutGroup.childForceExpandHeight = false;
        layoutGroup.padding = new RectOffset(0, 0, 0, 0);
    }
    
    /// <summary>
    /// Clear dynamically created stat fields (identified by name prefix).
    /// </summary>
    private void ClearDynamicStatFields(Transform container)
    {
        var toDestroy = new List<GameObject>();
        foreach (Transform child in container)
        {
            // Only use name prefix - no tag required
            if (child.name.StartsWith("DynStat_"))
            {
                toDestroy.Add(child.gameObject);
            }
        }
        foreach (var go in toDestroy)
        {
            Destroy(go);
        }
    }
    
    /// <summary>
    /// Create a stat field from a DisplayStat with tooltip support.
    /// </summary>
    private void CreateStatField(Transform container, VehicleComponent component, VehicleComponentUI.DisplayStat stat)
    {
        if (statFieldPrefab == null) return;
        
        var newField = Instantiate(statFieldPrefab, container);
        newField.name = $"DynStat_{stat.Name}";
        
        // Add LayoutElement for proper sizing in horizontal layout
        var layoutElement = newField.GetComponent<LayoutElement>();
        if (layoutElement == null)
        {
            layoutElement = newField.AddComponent<LayoutElement>();
        }
        layoutElement.minWidth = 60f;
        layoutElement.preferredWidth = 80f;
        layoutElement.flexibleWidth = 0f;
        
        var statField = newField.GetComponent<ComponentStatField>();
        if (statField != null)
        {
            // Configure with DisplayStat (includes attribute for tooltip)
            statField.Configure(stat);
            
            // Update display with entity and stat data
            statField.UpdateDisplay(component, stat, true);
        }
        else
        {
            // Fallback: find label and value text manually
            var labelText = newField.transform.Find("Label")?.GetComponent<TMP_Text>()
                         ?? newField.transform.Find("StatLabel")?.GetComponent<TMP_Text>();
            var valueText = newField.transform.Find("Value")?.GetComponent<TMP_Text>()
                         ?? newField.transform.Find("StatValue")?.GetComponent<TMP_Text>()
                         ?? newField.GetComponentInChildren<TMP_Text>();
            
            if (labelText != null) labelText.text = stat.Label;
            if (valueText != null) valueText.text = stat.Value;
        }
    }
    
    // ==================== LEGACY TEXT SECTIONS ====================
    
    private void PopulateSkills()
    {
        if (skillsSectionText == null) return;
        
        var seats = selectedVehicle.seats;
        int totalSkills = 0;
        
        // Count total skills from all seats
        foreach (var seat in seats)
        {
            if (seat == null) continue;
            foreach (var component in seat.GetOperationalComponents())
            {
                totalSkills += component.GetAllSkills().Count;
            }
            if (seat.assignedCharacter != null)
            {
                totalSkills += seat.assignedCharacter.GetPersonalSkills().Count;
            }
        }
        
        string info = $"<b>CREW & SKILLS ({seats.Count} seats, {totalSkills} skills):</b>\n";
        
        if (seats.Count == 0)
        {
            info += "  <color=#888888>No seats configured</color>\n";
        }
        else
        {
            foreach (var seat in seats)
            {
                if (seat == null) continue;
                
                // Seat status icon
                string statusIcon;
                string statusColor;
                if (!seat.CanAct())
                {
                    statusIcon = "❌";
                    statusColor = "#FF6666";
                }
                else if (seat.HasActedThisTurn())
                {
                    statusIcon = "☑️";
                    statusColor = "#888888";
                }
                else
                {
                    statusIcon = "✅";
                    statusColor = "#66FF66";
                }
                
                // Character name
                string characterName = seat.assignedCharacter?.characterName ?? "<color=#FF6666>Unassigned</color>";
                
                // Roles enabled by this seat
                RoleType roles = seat.GetEnabledRoles();
                string roleText = roles != RoleType.None ? $" [{roles}]" : "";
                
                info += $"\n  <b><color={statusColor}>{statusIcon} {seat.seatName}</color></b>{roleText}\n";
                info += $"    Operator: {characterName}\n";
                
                // Show reason if can't act
                if (!seat.CanAct())
                {
                    string reason = seat.GetCannotActReason();
                    info += $"    <color=#FF6666>⚠ {reason}</color>\n";
                }
                
                // Gather all skills for this seat
                var seatSkills = new List<Skill>();
                foreach (var component in seat.GetOperationalComponents())
                {
                    seatSkills.AddRange(component.GetAllSkills());
                }
                if (seat.assignedCharacter != null)
                {
                    seatSkills.AddRange(seat.assignedCharacter.GetPersonalSkills());
                }
                
                if (seatSkills.Count > 0)
                {
                    foreach (var skill in seatSkills)
                    {
                        if (skill != null)
                        {
                            bool canAfford = selectedVehicle.energy >= skill.energyCost;
                            string affordText = canAfford ? "" : " <color=#FF4444>(Can't afford)</color>";
                            info += $"    • <b>{skill.name}</b> ({skill.energyCost} EN){affordText}\n";
                        }
                    }
                }
                else
                {
                    info += $"    <color=#888888>No skills available</color>\n";
                }
            }
        }
        
        skillsSectionText.text = info;
    }
    
    private void PopulateEventHistory()
    {
        if (eventHistorySectionText == null) return;
        
        var vehicleEvents = RaceHistory.GetVehicleEvents(selectedVehicle);
        string info = $"<b>EVENT HISTORY ({vehicleEvents.Count} events):</b>\n";
        
        if (vehicleEvents.Count == 0)
        {
            info += "  <color=#888888>No events recorded</color>\n";
        }
        else
        {
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
        
        eventHistorySectionText.text = info;
    }
    
    // ==================== HELPER METHODS ====================
    
    private string GetStatusString(VehicleStatus status)
    {
        return status switch
        {
            VehicleStatus.Active => "Active",
            VehicleStatus.Destroyed => "Destroyed",
            _ => status.ToString()
        };
    }
    
    private Color GetStatusColor(VehicleStatus status)
    {
        return status switch
        {
            VehicleStatus.Active => new Color(0.27f, 1f, 0.27f),
            VehicleStatus.Destroyed => new Color(1f, 0.27f, 0.27f),
            _ => Color.white
        };
    }
    
    private string GetExposureString(ComponentExposure exposure)
    {
        return exposure switch
        {
            ComponentExposure.External => "External",
            ComponentExposure.Protected => "Protected",
            ComponentExposure.Internal => "Internal",
            ComponentExposure.Shielded => "Shielded",
            _ => "Unknown"
        };
    }
    
    private Color GetExposureColor(ComponentExposure exposure)
    {
        return exposure switch
        {
            ComponentExposure.External => new Color(0.27f, 1f, 0.27f),
            ComponentExposure.Protected => new Color(1f, 0.67f, 0.27f),
            ComponentExposure.Internal => new Color(1f, 0.53f, 0.27f),
            ComponentExposure.Shielded => new Color(0.53f, 0.87f, 1f),
            _ => Color.white
        };
    }
}