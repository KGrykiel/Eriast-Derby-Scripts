using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Logging;

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

    [Header("Settings")]
    public int maxDisplayedEvents = 100; // Limit to prevent performance issues
    public bool autoScrollToBottom = true;
    
    [Header("Tooltip Settings")]
    [Tooltip("Enable hover tooltips for detailed breakdowns")]
    public bool enableTooltips = true;

    private ScrollRect scrollRect;
    private int lastProcessedEventCount = 0;
    private List<RaceEvent> currentlyDisplayedEvents = new();

    void Start()
    {
        scrollRect = GetComponentInParent<ScrollRect>();

        criticalToggle?.onValueChanged.AddListener(_ => FullRefreshFeed());
        highToggle?.onValueChanged.AddListener(_ => FullRefreshFeed());
        mediumToggle?.onValueChanged.AddListener(_ => FullRefreshFeed());
        lowToggle?.onValueChanged.AddListener(_ => FullRefreshFeed());
        debugToggle?.onValueChanged.AddListener(_ => FullRefreshFeed());

        FullRefreshFeed();
    }

    void Update()
    {
        int currentEventCount = RaceHistory.Instance.AllEvents.Count;

        if (currentEventCount > lastProcessedEventCount)
        {
            AppendNewEvents();
            lastProcessedEventCount = currentEventCount;
        }
    }

    private void AppendNewEvents()
    {
        var filteredEvents = GetFilteredEvents();

        var newEvents = filteredEvents
            .Skip(currentlyDisplayedEvents.Count)
            .ToList();

        if (newEvents.Count == 0)
            return;

        bool wasAtBottom = scrollRect != null && scrollRect.verticalNormalizedPosition <= 0.01f;

        foreach (var evt in newEvents)
        {
            CreateEventEntry(evt);
            currentlyDisplayedEvents.Add(evt);
        }

        while (currentlyDisplayedEvents.Count > maxDisplayedEvents)
        {
            if (contentContainer.childCount > 0)
                Destroy(contentContainer.GetChild(0).gameObject);
            currentlyDisplayedEvents.RemoveAt(0);
        }

        if (autoScrollToBottom || wasAtBottom)
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

        lastProcessedEventCount = RaceHistory.Instance.AllEvents.Count;
    }

    public void RefreshFeed()
    {
        FullRefreshFeed();
    }

    private List<RaceEvent> GetFilteredEvents()
    {
        var events = RaceHistory.Instance.AllEvents.ToList();

        return events.Where(evt =>
        {
            return evt.importance switch
            {
                EventImportance.Critical => criticalToggle == null || criticalToggle.isOn,
                EventImportance.High => highToggle == null || highToggle.isOn,
                EventImportance.Medium => mediumToggle == null || mediumToggle.isOn,
                EventImportance.Low => lowToggle != null && lowToggle.isOn,
                EventImportance.Debug => debugToggle != null && debugToggle.isOn,
                _ => false
            };
        }).ToList();
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
                displayText += " <color=#6688FF>[?]</color>";

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
               evt.metadata.ContainsKey("effectBreakdown") ||        // Status effects
               evt.metadata.ContainsKey("dcBreakdown") ||            // Saving throw DC
               evt.metadata.ContainsKey("saveModifiersBreakdown") || // Save modifiers
               evt.metadata.ContainsKey("componentRollBreakdown") ||
               evt.metadata.ContainsKey("chassisRollBreakdown") ||
               evt.metadata.ContainsKey("defenseBreakdown") ||       // AC breakdown
               evt.metadata.ContainsKey("baseRoll") ||
               evt.metadata.ContainsKey("modifierCount") ||
               evt.metadata.ContainsKey("componentCount") ||
               evt.metadata.ContainsKey("totalDamage") ||
               evt.metadata.ContainsKey("actualChange");             // Restoration events
    }
}