using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using RacingGame.Events;

/// <summary>
/// Displays filtered event feed to the DM.
/// Updates in real-time as new events are logged.
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

    private ScrollRect scrollRect;
    private int lastProcessedEventCount = 0;

    void Start()
    {
        scrollRect = GetComponentInParent<ScrollRect>();

        // Setup toggle listeners
        criticalToggle?.onValueChanged.AddListener(_ => RefreshFeed());
        highToggle?.onValueChanged.AddListener(_ => RefreshFeed());
        mediumToggle?.onValueChanged.AddListener(_ => RefreshFeed());
        lowToggle?.onValueChanged.AddListener(_ => RefreshFeed());
        debugToggle?.onValueChanged.AddListener(_ => RefreshFeed());

        // Initial refresh
        RefreshFeed();
    }

    void Update()
    {
        // Check if new events have been added
        int currentEventCount = RaceHistory.Instance.AllEvents.Count;

        if (currentEventCount != lastProcessedEventCount)
        {
            RefreshFeed();
            lastProcessedEventCount = currentEventCount;
        }
    }

    public void RefreshFeed()
    {
        // Clear existing entries
        foreach (Transform child in contentContainer)
        {
            Destroy(child.gameObject);
        }

        // Get filtered events
        var filteredEvents = GetFilteredEvents();

        // Limit display count
        var displayEvents = filteredEvents.TakeLast(maxDisplayedEvents).ToList();

        // Create UI entries
        foreach (var evt in displayEvents)
        {
            CreateEventEntry(evt);
        }

        // Auto-scroll to bottom
        if (autoScrollToBottom)
        {
            Canvas.ForceUpdateCanvases();
            scrollRect.verticalNormalizedPosition = 0f;
        }

        lastProcessedEventCount = RaceHistory.Instance.AllEvents.Count;
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
            text.text = evt.GetFormattedText(includeTimestamp: true, includeLocation: true);
            text.fontSize = 12;
            text.enableWordWrapping = true;
        }

        // Set layout
        var layoutElement = entryObj.GetComponent<LayoutElement>();
        if (layoutElement == null)
        {
            layoutElement = entryObj.AddComponent<LayoutElement>();
        }
        layoutElement.preferredHeight = 25;
        layoutElement.flexibleWidth = 1;
    }
}