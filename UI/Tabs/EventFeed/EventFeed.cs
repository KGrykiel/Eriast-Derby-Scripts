using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using RacingGame.Events;

/// <summary>
/// Displays filtered event feed to the DM.
/// Updates in real-time as new events are logged.
/// Supports hover tooltips for detailed roll/damage breakdowns.
/// </summary>
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
    private List<RaceEvent> currentlyDisplayedEvents = new List<RaceEvent>();

    void Start()
    {
        scrollRect = GetComponentInParent<ScrollRect>();

        // Setup toggle listeners - full refresh needed when filters change
        criticalToggle?.onValueChanged.AddListener(_ => FullRefreshFeed());
        highToggle?.onValueChanged.AddListener(_ => FullRefreshFeed());
        mediumToggle?.onValueChanged.AddListener(_ => FullRefreshFeed());
        lowToggle?.onValueChanged.AddListener(_ => FullRefreshFeed());
        debugToggle?.onValueChanged.AddListener(_ => FullRefreshFeed());

        // Initial refresh
        FullRefreshFeed();
    }

    void Update()
    {
        // Check if new events have been added
        int currentEventCount = RaceHistory.Instance.AllEvents.Count;

        if (currentEventCount > lastProcessedEventCount)
        {
            AppendNewEvents();
            lastProcessedEventCount = currentEventCount;
        }
    }

    /// <summary>
    /// Appends only new events that haven't been displayed yet.
    /// Preserves scroll position and existing entries.
    /// </summary>
    private void AppendNewEvents()
    {
        // Get all filtered events
        var filteredEvents = GetFilteredEvents();

        // Find events that are new (not in currentlyDisplayedEvents)
        var newEvents = filteredEvents
            .Skip(currentlyDisplayedEvents.Count)
            .ToList();

        if (newEvents.Count == 0)
            return;

        // Track scroll position before adding
        bool wasAtBottom = scrollRect != null && scrollRect.verticalNormalizedPosition <= 0.01f;

        // Add new events
        foreach (var evt in newEvents)
        {
            CreateEventEntry(evt);
            currentlyDisplayedEvents.Add(evt);
        }

        // Enforce max limit (remove oldest if exceeded)
        while (currentlyDisplayedEvents.Count > maxDisplayedEvents)
        {
            // Remove oldest entry from UI
            if (contentContainer.childCount > 0)
            {
                Destroy(contentContainer.GetChild(0).gameObject);
            }
            currentlyDisplayedEvents.RemoveAt(0);
        }

        // Auto-scroll to bottom if user was already at bottom, or if autoScroll is enabled
        if (autoScrollToBottom || wasAtBottom)
        {
            Canvas.ForceUpdateCanvases();
            if (scrollRect != null)
            {
                scrollRect.verticalNormalizedPosition = 0f;
            }
        }
    }

    /// <summary>
    /// Full refresh of the feed. Used when filters change.
    /// </summary>
    private void FullRefreshFeed()
    {
        // Clear existing entries
        foreach (Transform child in contentContainer)
        {
            Destroy(child.gameObject);
        }

        currentlyDisplayedEvents.Clear();

        // Get filtered events
        var filteredEvents = GetFilteredEvents();

        // Limit display count
        var displayEvents = filteredEvents.TakeLast(maxDisplayedEvents).ToList();

        // Create UI entries
        foreach (var evt in displayEvents)
        {
            CreateEventEntry(evt);
            currentlyDisplayedEvents.Add(evt);
        }

        // Auto-scroll to bottom
        if (autoScrollToBottom)
        {
            Canvas.ForceUpdateCanvases();
            if (scrollRect != null)
            {
                scrollRect.verticalNormalizedPosition = 0f;
            }
        }

        lastProcessedEventCount = RaceHistory.Instance.AllEvents.Count;
    }

    /// <summary>
    /// Public method to manually refresh the entire feed.
    /// </summary>
    public void RefreshFeed()
    {
        FullRefreshFeed();
    }

    private List<RaceEvent> GetFilteredEvents()
    {
        var events = RaceHistory.Instance.AllEvents.ToList();

        // Filter by importance based on toggle states
        return events.Where(evt =>
        {
            return evt.importance switch
            {
                EventImportance.Critical => criticalToggle?.isOn ?? true,
                EventImportance.High => highToggle?.isOn ?? true,
                EventImportance.Medium => mediumToggle?.isOn ?? true,
                EventImportance.Low => lowToggle?.isOn ?? false,
                EventImportance.Debug => debugToggle?.isOn ?? false,
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
            // Fallback: create basic TextMeshPro
            entryObj = new GameObject("EventEntry");
            entryObj.transform.SetParent(contentContainer);
            entryObj.AddComponent<TextMeshProUGUI>();
        }

        var text = entryObj.GetComponent<TextMeshProUGUI>();
        if (text != null)
        {
            // Build display text - add indicator if has breakdown data
            string displayText = evt.GetFormattedText(includeTimestamp: true, includeLocation: true);
            
            // Add hover indicator if event has breakdown metadata
            if (enableTooltips && HasBreakdownData(evt))
            {
                displayText += " <color=#6688FF>[?]</color>";
            }
            
            text.text = displayText;
            text.fontSize = 12;
            text.textWrappingMode = TextWrappingModes.Normal;
            
            // Enable raycast target for hover detection
            text.raycastTarget = true;
        }

        // Set layout
        var layoutElement = entryObj.GetComponent<LayoutElement>();
        if (layoutElement == null)
        {
            layoutElement = entryObj.AddComponent<LayoutElement>();
        }
        layoutElement.preferredHeight = 25;
        layoutElement.flexibleWidth = 1;
        
        // Add hover component for tooltips
        if (enableTooltips && HasBreakdownData(evt))
        {
            var hoverComponent = entryObj.AddComponent<EventEntryHover>();
            hoverComponent.SetEvent(evt);
        }
    }
    
    /// <summary>
    /// Checks if an event has breakdown data worth showing in tooltip.
    /// </summary>
    private bool HasBreakdownData(RaceEvent evt)
    {
        if (evt == null || evt.metadata == null)
            return false;
            
        // Check for various breakdown metadata keys
        return evt.metadata.ContainsKey("rollBreakdown") ||
               evt.metadata.ContainsKey("damageBreakdown") ||
               evt.metadata.ContainsKey("componentRollBreakdown") ||
               evt.metadata.ContainsKey("chassisRollBreakdown") ||
               evt.metadata.ContainsKey("baseRoll") ||
               evt.metadata.ContainsKey("modifierCount") ||
               evt.metadata.ContainsKey("componentCount") ||
               evt.metadata.ContainsKey("totalDamage");
    }
}