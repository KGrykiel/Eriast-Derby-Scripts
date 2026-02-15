using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Logging;
using Assets.Scripts.UI.Components;
using Assets.Scripts.Entities.Vehicle;

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
    public TMP_Text vehicleSizeValueText;
    
    [Header("Components Section")]
    public Transform componentListContainer;
    public GameObject componentEntryPrefab;
    
    [Header("Dynamic Stat Field")]
    [Tooltip("Prefab for dynamically created stat fields (must have ComponentStatField component)")]
    public GameObject statFieldPrefab;
    
    [Header("Legacy Text Sections")]
    public TMP_Text skillsSectionText;
    public TMP_Text eventHistorySectionText;
    
    private List<Vehicle> allVehicles = new();
    private Vehicle selectedVehicle;
    private Vehicle pendingVehicleSelection;
    private bool initialized = false;

    private List<GameObject> componentEntryInstances = new();

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
        if (initialized)
        {
            if (pendingVehicleSelection != null && allVehicles != null)
            {
                int index = allVehicles.IndexOf(pendingVehicleSelection);
                if (index >= 0)
                {
                    selectedVehicle = pendingVehicleSelection;
                    if (vehicleDropdown != null)
                        vehicleDropdown.value = index;
                }
                pendingVehicleSelection = null;
            }
            RefreshDetails();
        }
        else
        {
            TryInitialize();
        }
    }

    void Update()
    {
        if (!initialized)
        {
            TryInitialize();
            return;
        }

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

                if (RaceHistory.Instance != null)
                    lastEventCount = RaceHistory.Instance.AllEvents.Count;
            }
        }
    }
    
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
  
        List<string> options = new();
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
            // Use pending selection if set, otherwise default to first
            if (pendingVehicleSelection != null && allVehicles.Contains(pendingVehicleSelection))
            {
                selectedVehicle = pendingVehicleSelection;
                int index = allVehicles.IndexOf(pendingVehicleSelection);
                vehicleDropdown.SetValueWithoutNotify(index);
                pendingVehicleSelection = null;
            }
            else
            {
                selectedVehicle = allVehicles[0];
            }
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
    
    public void SelectVehicle(Vehicle vehicle)
    {
        if (vehicle == null) return;

        if (!gameObject.activeInHierarchy)
        {
            pendingVehicleSelection = vehicle;
            return;
        }

        if (!initialized || allVehicles == null || allVehicles.Count == 0)
        {
            pendingVehicleSelection = vehicle;
            TryInitialize();
            return;
        }

        int index = allVehicles.IndexOf(vehicle);
        if (index >= 0)
        {
            selectedVehicle = vehicle;
            if (vehicleDropdown != null)
                vehicleDropdown.value = index;
            RefreshDetails();
        }
        else
        {
            selectedVehicle = vehicle;
            RefreshDetails();
        }
    }
 
    public void RefreshDetails()
    {
        if (selectedVehicle == null) return;
        
        PopulateBasicInfo();
        PopulateStats();
        PopulateComponents();
        PopulateSkills();
        PopulateEventHistory();
    }
    
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
    
    private void PopulateStats()
    {
        if (vehicleHPValueText != null)
        {
            var hpDisplay = vehicleHPValueText.GetComponent<StatValueDisplay>();
            int currentHealth = selectedVehicle.chassis?.GetCurrentHealth() ?? 0;
            int maxHealth = selectedVehicle.chassis?.GetMaxHealth() ?? 0;
            int baseMaxHP = selectedVehicle.chassis?.GetBaseMaxHealth() ?? 100;

            if (hpDisplay != null)
            {
                hpDisplay.UpdateDisplay(
                    selectedVehicle.chassis,
                    Attribute.MaxHealth,
                    baseMaxHP,
                    maxHealth,
                    $"{currentHealth}/{maxHealth}"
                );
            }
            else
            {
                vehicleHPValueText.text = $"{currentHealth}/{maxHealth}";
            }
            
            if (vehicleHPBar != null)
            {
                float percent = maxHealth > 0 
                    ? (float)currentHealth / maxHealth 
                    : 0f;
                vehicleHPBar.value = percent;
            }
        }
        
        if (vehicleEnergyValueText != null)
        {
            var energyDisplay = vehicleEnergyValueText.GetComponent<StatValueDisplay>();
            int currentEnergy = selectedVehicle.powerCore?.GetCurrentEnergy() ?? 0;
            int maxEnergy = selectedVehicle.powerCore?.GetMaxEnergy() ?? 0;
            int baseMaxEnergy = selectedVehicle.powerCore?.GetBaseMaxEnergy() ?? 100;
            
            if (energyDisplay != null)
            {
                energyDisplay.UpdateDisplay(
                    selectedVehicle.powerCore,
                    Attribute.MaxEnergy,
                    baseMaxEnergy,
                    maxEnergy,
                    $"{currentEnergy}/{maxEnergy}"
                );
            }
            else
            {
                vehicleEnergyValueText.text = $"{currentEnergy}/{maxEnergy}";
            }
            
            if (vehicleEnergyBar != null)
            {
                float percent = maxEnergy > 0 
                    ? (float)currentEnergy / maxEnergy 
                    : 0f;
                vehicleEnergyBar.value = percent;
            }
        }
        
        if (vehicleSpeedValueText != null)
        {
            var drive = selectedVehicle.GetDriveComponent();

            float speed = drive?.GetCurrentSpeed() ?? 0f;
            vehicleSpeedValueText.text = $"{speed:F1}";
        }
        
        if (vehicleACValueText != null)
        {
            var acDisplay = vehicleACValueText.GetComponent<StatValueDisplay>();
            int modifiedAC = selectedVehicle.chassis?.GetArmorClass() ?? 10;
            
            if (acDisplay != null && selectedVehicle.chassis != null)
            {
                int baseAC = selectedVehicle.chassis.GetBaseArmorClass();
                
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
                acDisplay.UpdateDisplaySimple(10, 10, modifiedAC.ToString());
            }
            else
            {
                vehicleACValueText.text = modifiedAC.ToString();
            }
        }
        
        if (vehicleEnergyRegenValueText != null)
        {
            var regenDisplay = vehicleEnergyRegenValueText.GetComponent<StatValueDisplay>();
            int modifiedRegen = selectedVehicle.powerCore?.GetEnergyRegen() ?? 0;
            
            if (regenDisplay != null && selectedVehicle.powerCore != null)
            {
                int baseRegen = selectedVehicle.powerCore.GetBaseEnergyRegen();
                
                regenDisplay.UpdateDisplay(
                    selectedVehicle.powerCore,
                    Attribute.EnergyRegen,
                    baseRegen,
                    modifiedRegen,
                    $"{modifiedRegen}"
                );
            }
            else if (regenDisplay != null)
            {
                regenDisplay.UpdateDisplaySimple(0, 0, $"{modifiedRegen}");
            }
            else
            {
                vehicleEnergyRegenValueText.text = $"{modifiedRegen}";
            }
        }
        
        if (vehicleSizeValueText != null && selectedVehicle.chassis != null)
        {
            var sizeDisplay = vehicleSizeValueText.GetComponent<SizeDisplay>();
            var sizeCategory = selectedVehicle.chassis.sizeCategory;
            string sizeText = sizeCategory.ToString();
            Color sizeColor = GetSizeColor(sizeCategory);
            string tooltip = BuildSizeTooltip(sizeCategory);
            
            if (sizeDisplay != null)
            {
                sizeDisplay.UpdateDisplay(sizeCategory, sizeColor, tooltip);
            }
            else
            {
                vehicleSizeValueText.text = sizeText;
                vehicleSizeValueText.color = sizeColor;
            }
        }
    }
    
    private void PopulateComponents()
    {
        if (componentListContainer == null || componentEntryPrefab == null)
        {
            Debug.LogWarning("[VehicleInspectorPanel] Component list container or prefab not assigned!");
            return;
        }
        
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
        
        foreach (var entry in componentEntryInstances)
        {
            if (entry != null)
            {
                Destroy(entry);
            }
        }
        componentEntryInstances.Clear();
        
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
        
        StartCoroutine(ForceLayoutRebuild());
    }
    
    private System.Collections.IEnumerator ForceLayoutRebuild()
    {
        yield return null;

        if (componentListContainer != null)
        {
            var rectTransform = componentListContainer as RectTransform;
            LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
        }
        
        Transform content = componentListContainer != null ? componentListContainer.parent : null;
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
        var layoutElement = entryObj.GetComponent<LayoutElement>();
        if (layoutElement == null)
        {
            layoutElement = entryObj.AddComponent<LayoutElement>();
        }
        layoutElement.minHeight = 80;
        layoutElement.preferredHeight = -1;
        
        var componentIcon = entryObj.transform.Find("ComponentHeader/ComponentIcon")?.GetComponent<Image>();
        var componentName = entryObj.transform.Find("ComponentHeader/ComponentName")?.GetComponent<TMP_Text>();
        var destroyedIcon = entryObj.transform.Find("ComponentHeader/StatusIconGroup/DestroyedIcon");
        var disabledIcon = entryObj.transform.Find("ComponentHeader/StatusIconGroup/DisabledIcon");
        
        if (componentName != null)
        {
            componentName.text = component.name;
        }
        
        if (destroyedIcon != null)
        {
            destroyedIcon.gameObject.SetActive(component.isDestroyed);
        }
        
        if (disabledIcon != null)
        {
            bool showDisabled = !component.IsOperational;
            disabledIcon.gameObject.SetActive(showDisabled);
        }
        
        var exposureValueText = entryObj.transform.Find("ExposureRow/ExposureValueText")?.GetComponent<TMP_Text>()
                             ?? entryObj.transform.Find("ComponentHeader/ExposureValueText")?.GetComponent<TMP_Text>()
                             ?? entryObj.transform.Find("ExposureValueText")?.GetComponent<TMP_Text>();
        
        if (exposureValueText != null)
        {
            exposureValueText.text = GetExposureString(component.exposure);
            exposureValueText.color = GetExposureColor(component.exposure);
        }
        
        var hpValueText = entryObj.transform.Find("ComponentStatsRow/HPGroup/HPValueText")?.GetComponent<TMP_Text>();
        var hpBar = entryObj.transform.Find("ComponentStatsRow/HPGroup/HPBar")?.GetComponent<Slider>();
        var acValueText = entryObj.transform.Find("ComponentStatsRow/ACGroup/ACValueText")?.GetComponent<TMP_Text>();
        
        if (hpValueText != null)
        {
            var hpDisplay = hpValueText.GetComponent<StatValueDisplay>();
            if (hpDisplay != null)
            {
                hpDisplay.UpdateDisplay(
                    component,
                    Attribute.MaxHealth,
                    component.GetBaseMaxHealth(),
                    component.GetMaxHealth(),
                    $"{component.GetCurrentHealth()}/{component.GetMaxHealth()}"
                );
            }
            else
            {
                hpValueText.text = $"{component.GetCurrentHealth()}/{component.GetMaxHealth()}";
            }
        }
        
        if (hpBar != null)
        {
            int maxHP = component.GetMaxHealth();
            float percent = maxHP > 0 
                ? (float)component.GetCurrentHealth() / (float)maxHP 
                : 0f;
            hpBar.minValue = 0f;
            hpBar.maxValue = 1f;
            hpBar.value = percent;
        }
        
        if (acValueText != null)
        {
            int baseAC = component.GetBaseArmorClass();
            int modifiedAC = component.GetArmorClass();
            
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
        
        var dynamicStatsContainer = entryObj.transform.Find("ComponentStatsRow/DynamicStatsContainer")
                                 ?? entryObj.transform.Find("ComponentStatsRow/DynamicStats")
                                 ?? entryObj.transform.Find("DynamicStatsRow");
        
        if (dynamicStatsContainer != null)
        {
            ConfigureStatsContainerLayout(dynamicStatsContainer);
            ClearDynamicStatFields(dynamicStatsContainer);

            var displayStats = component.GetDisplayStats();
            
            foreach (var stat in displayStats)
            {
                CreateStatField(dynamicStatsContainer, component, stat);
            }
        }
        
        var statusBar = entryObj.transform.Find("ComponentStatusEffectBar")?.GetComponent<StatusEffectBar>();
        if (statusBar != null)
        {
            statusBar.SetEntity(component);
            statusBar.Refresh();
        }
        
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
                    warningText.text = $"{reason}";
                }
            }
        }
    }
    
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
    
    private void ClearDynamicStatFields(Transform container)
    {
        var toDestroy = new List<GameObject>();
        foreach (Transform child in container)
        {
            if (child.name.StartsWith("DynStat_"))
                toDestroy.Add(child.gameObject);
        }
        foreach (var go in toDestroy)
            Destroy(go);
    }
    
    private void CreateStatField(Transform container, VehicleComponent component, VehicleComponentUI.DisplayStat stat)
    {
        if (statFieldPrefab == null) return;

        var newField = Instantiate(statFieldPrefab, container);
        newField.name = $"DynStat_{stat.Name}";

        var layoutElement = newField.GetComponent<LayoutElement>();
        if (layoutElement == null)
            layoutElement = newField.AddComponent<LayoutElement>();
        layoutElement.minWidth = 60f;
        layoutElement.preferredWidth = 80f;
        layoutElement.flexibleWidth = 0f;
        
        var statField = newField.GetComponent<ComponentStatField>();
        if (statField != null)
        {
            statField.Configure(stat);
            statField.UpdateDisplay(component, stat, true);
        }
        else
        {
            var labelText = newField.transform.Find("Label")?.GetComponent<TMP_Text>()
                         ?? newField.transform.Find("StatLabel")?.GetComponent<TMP_Text>();
            var valueText = newField.transform.Find("Value")?.GetComponent<TMP_Text>()
                         ?? newField.transform.Find("StatValue")?.GetComponent<TMP_Text>()
                         ?? newField.GetComponentInChildren<TMP_Text>();
            
            if (labelText != null) labelText.text = stat.Label;
            if (valueText != null) valueText.text = stat.Value;
        }
    }
    
    private void PopulateSkills()
    {
        if (skillsSectionText == null) return;
        
        var seats = selectedVehicle.seats;
        int totalSkills = 0;

        foreach (var seat in seats)
        {
            if (seat == null) continue;
            foreach (var component in seat.GetOperationalComponents())
                totalSkills += component.GetAllSkills().Count;
            if (seat.assignedCharacter != null)
                totalSkills += seat.assignedCharacter.GetPersonalAbilities().Count;
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
                
                string statusIcon;
                string statusColor;
                if (!seat.CanAct())
                {
                    statusIcon = "X";
                    statusColor = "#FF6666";
                }
                else if (seat.HasActedThisTurn())
                {
                    statusIcon = "/";
                    statusColor = "#888888";
                }
                else
                {
                    statusIcon = "Y";
                    statusColor = "#66FF66";
                }
                
                string characterName = seat.assignedCharacter != null ? seat.assignedCharacter.characterName : null ?? "<color=#FF6666>Unassigned</color>";

                RoleType roles = seat.GetEnabledRoles();
                string roleText = roles != RoleType.None ? $" [{roles}]" : "";
                
                info += $"\n  <b><color={statusColor}>{statusIcon} {seat.seatName}</color></b>{roleText}\n";
                info += $"    Operator: {characterName}\n";
                
                if (!seat.CanAct())
                {
                    string reason = seat.GetCannotActReason();
                    info += $"    <color=#FF6666>{reason}</color>\n";
                }

                var seatSkills = new List<Skill>();
                foreach (var component in seat.GetOperationalComponents())
                {
                    seatSkills.AddRange(component.GetAllSkills());
                }
                if (seat.assignedCharacter != null)
                {
                    seatSkills.AddRange(seat.assignedCharacter.GetPersonalAbilities());
                }
                
                if (seatSkills.Count > 0)
                {
                    int currentEnergy = selectedVehicle.powerCore?.GetCurrentEnergy() ?? 0;
                    foreach (var skill in seatSkills)
                    {
                        if (skill != null)
                        {
                            bool canAfford = currentEnergy >= skill.energyCost;
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
    
    private string BuildSizeTooltip(VehicleSizeCategory size)
    {
        var modifiers = VehicleSizeModifiers.GetModifiers(size, null);
        
        string tooltip = $"<b>{size} Vehicle</b>\n\n";
        
        if (modifiers.Count == 0)
        {
            tooltip += "No modifiers (baseline)";
            return tooltip;
        }
        
        tooltip += "<b>Modifiers:</b>\n";
        
        foreach (var mod in modifiers)
        {
            string sign = mod.Value >= 0 ? "+" : "";
            string attrName = mod.Attribute.ToString();

            attrName = attrName switch
            {
                "ArmorClass" => "AC",
                "MaxSpeed" => "Max Speed",
                _ => attrName
            };
            
            tooltip += $"  {sign}{mod.Value} {attrName}\n";
        }
        return tooltip;
    }
    
    private Color GetSizeColor(VehicleSizeCategory size)
    {
        return size switch
        {
            VehicleSizeCategory.Tiny => new Color(0.4f, 1f, 0.4f),
            VehicleSizeCategory.Small => new Color(0.6f, 1f, 0.6f),
            VehicleSizeCategory.Medium => Color.white,
            VehicleSizeCategory.Large => new Color(1f, 0.7f, 0.4f),
            VehicleSizeCategory.Huge => new Color(1f, 0.5f, 0.3f),
            _ => Color.white
        };
    }
}