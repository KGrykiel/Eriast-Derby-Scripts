using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Entities.Vehicles;
using Assets.Scripts.Logging;
using Assets.Scripts.Managers;

public class EventFeed : MonoBehaviour
{
    [Header("UI References")]
    public Transform contentContainer; // The Content object of ScrollView
    public GameObject eventEntryPrefab; // TextMeshPro prefab for event entries

    [Header("Filter Toggles")]
    public Toggle criticalToggle;
    public Toggle highToggle;
    public Toggle mediumToggle;
    public Toggle lowToggle;
    public Toggle debugToggle;

    [Header("Vehicle Filter")]
    [Tooltip("Optional — filters events to a single vehicle. First option is always 'All Vehicles'.")]
    public TMP_Dropdown vehicleFilterDropdown;

    [Header("Settings")]
    public int maxDisplayedEvents = 100; // Limit to prevent performance issues
    public bool autoScrollToBottom = true;
    
    [Header("Tooltip Settings")]
    [Tooltip("Enable hover tooltips for detailed breakdowns")]
    public bool enableTooltips = true;

    private ScrollRect scrollRect;
    private int lastProcessedEventCount = 0;
    private readonly List<RaceEvent> currentlyDisplayedEvents = new();
    private Vehicle filteredVehicle;

    void Start()
    {
        scrollRect = GetComponentInParent<ScrollRect>();

        criticalToggle?.onValueChanged.AddListener(_ => FullRefreshFeed());
        highToggle?.onValueChanged.AddListener(_ => FullRefreshFeed());
        mediumToggle?.onValueChanged.AddListener(_ => FullRefreshFeed());
        lowToggle?.onValueChanged.AddListener(_ => FullRefreshFeed());
        debugToggle?.onValueChanged.AddListener(_ => FullRefreshFeed());

        vehicleFilterDropdown.onValueChanged.AddListener(_ => OnVehicleFilterChanged());
        PopulateVehicleFilter();

        FullRefreshFeed();
    }

    void Update()
    {
        int currentEventCount = RaceHistory.AllEvents.Count;

        if (currentEventCount > lastProcessedEventCount)
        {
            AppendNewEvents();
            lastProcessedEventCount = currentEventCount;
        }
    }

    private void AppendNewEvents()
    {
        var allEvents = RaceHistory.AllEvents;

        bool wasAtBottom = scrollRect != null && scrollRect.verticalNormalizedPosition <= 0.01f;
        bool addedAny = false;

        for (int i = lastProcessedEventCount; i < allEvents.Count; i++)
        {
            var evt = allEvents[i];
            if (!IsEventVisible(evt)) continue;

            CreateEventEntry(evt);
            currentlyDisplayedEvents.Add(evt);
            addedAny = true;

            if (currentlyDisplayedEvents.Count > maxDisplayedEvents)
            {
                if (contentContainer.childCount > 0)
                    Destroy(contentContainer.GetChild(0).gameObject);
                currentlyDisplayedEvents.RemoveAt(0);
            }
        }

        if (addedAny && (autoScrollToBottom || wasAtBottom))
        {
            Canvas.ForceUpdateCanvases();
            if (scrollRect != null)
                scrollRect.verticalNormalizedPosition = 0f;
        }
    }

    private void FullRefreshFeed()
    {
        foreach (Transform child in contentContainer)
            Destroy(child.gameObject);

        currentlyDisplayedEvents.Clear();

        var filteredEvents = GetFilteredEvents();
        var displayEvents = filteredEvents.TakeLast(maxDisplayedEvents).ToList();

        foreach (var evt in displayEvents)
        {
            CreateEventEntry(evt);
            currentlyDisplayedEvents.Add(evt);
        }

        if (autoScrollToBottom)
        {
            Canvas.ForceUpdateCanvases();
            if (scrollRect != null)
                scrollRect.verticalNormalizedPosition = 0f;
        }

        lastProcessedEventCount = RaceHistory.AllEvents.Count;
    }

    public void RefreshFeed()
    {
        FullRefreshFeed();
    }

    private bool IsEventVisible(RaceEvent evt)
    {
        bool importanceVisible = evt.importance switch
        {
            EventImportance.Critical => criticalToggle == null || criticalToggle.isOn,
            EventImportance.High => highToggle == null || highToggle.isOn,
            EventImportance.Medium => mediumToggle == null || mediumToggle.isOn,
            EventImportance.Low => lowToggle != null && lowToggle.isOn,
            EventImportance.Debug => debugToggle != null && debugToggle.isOn,
            _ => false
        };

        if (!importanceVisible) return false;

        if (filteredVehicle != null)
            return evt.involvedVehicles != null && evt.involvedVehicles.Contains(filteredVehicle);

        return true;
    }

    private List<RaceEvent> GetFilteredEvents()
    {
        return RaceHistory.AllEvents.Where(IsEventVisible).ToList();
    }

    // ==================== VEHICLE FILTER ====================

    private readonly List<Vehicle> vehicleFilterList = new();

    private void PopulateVehicleFilter()
    {
        vehicleFilterDropdown.ClearOptions();
        vehicleFilterList.Clear();

        var vehicles = RacePositionTracker.GetAll();
        var options = new List<string> { "All Vehicles" };
        foreach (var vehicle in vehicles)
        {
            if (vehicle == null) continue;
            vehicleFilterList.Add(vehicle);
            options.Add(vehicle.vehicleName);
        }

        vehicleFilterDropdown.AddOptions(options);
        vehicleFilterDropdown.SetValueWithoutNotify(0);
        filteredVehicle = null;
    }

    private void OnVehicleFilterChanged()
    {
        int index = vehicleFilterDropdown.value;
        if (index <= 0 || index > vehicleFilterList.Count)
            filteredVehicle = null;
        else
            filteredVehicle = vehicleFilterList[index - 1];

        FullRefreshFeed();
    }

    private void CreateEventEntry(RaceEvent evt)
    {
        GameObject entryObj;

        if (eventEntryPrefab != null)
        {
            entryObj = Instantiate(eventEntryPrefab, contentContainer);
        }
        else
        {
            entryObj = new GameObject("EventEntry");
            entryObj.transform.SetParent(contentContainer);
            entryObj.AddComponent<TextMeshProUGUI>();
        }

        var text = entryObj.GetComponent<TextMeshProUGUI>();
        if (text != null)
        {
            string displayText = evt.GetFormattedText(includeTimestamp: true, includeLocation: true);

            if (enableTooltips && HasBreakdownData(evt))
                displayText += $" <color={LogColors.IconUnknown}>[?]</color>";

            text.text = displayText;
            text.fontSize = 12;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.raycastTarget = true;
        }

        var layoutElement = entryObj.GetComponent<LayoutElement>();
        if (layoutElement == null)
            layoutElement = entryObj.AddComponent<LayoutElement>();
        layoutElement.preferredHeight = 25;
        layoutElement.flexibleWidth = 1;

        if (enableTooltips && HasBreakdownData(evt))
        {
            var hoverComponent = entryObj.AddComponent<EventEntryHover>();
            hoverComponent.SetEvent(evt);
        }
    }
    
    private bool HasBreakdownData(RaceEvent evt)
    {
        if (evt == null || evt.metadata == null)
            return false;

        return evt.metadata.ContainsKey("rollBreakdown") ||
               evt.metadata.ContainsKey("damageBreakdown") ||
               evt.metadata.ContainsKey("restorationBreakdown") ||
               evt.metadata.ContainsKey("effectBreakdown") ||
               evt.metadata.ContainsKey("dcBreakdown") ||
               evt.metadata.ContainsKey("defenseBreakdown") ||
               evt.metadata.ContainsKey("aiDecision");
    }
}