using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace RacingGame.Events
{
    /// <summary>
    /// Central event logging system for the race.
    /// Replaces SimulationLogger with structured, filterable event tracking.
    /// Supports highlight reel generation and per-vehicle narratives.
    /// </summary>
    public class RaceHistory : MonoBehaviour
    {
        private static RaceHistory instance;
        public static RaceHistory Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject go = new GameObject("RaceHistory");
                    instance = go.AddComponent<RaceHistory>();
                    DontDestroyOnLoad(go);
                }
                return instance;
            }
        }
        
        /// <summary>
        /// All events that have occurred in the race.
        /// </summary>
        private List<RaceEvent> allEvents = new List<RaceEvent>();
        
        /// <summary>
        /// Events indexed by vehicle for quick per-vehicle lookups.
        /// </summary>
        private Dictionary<Vehicle, List<RaceEvent>> vehicleEvents = new Dictionary<Vehicle, List<RaceEvent>>();
        
        /// <summary>
        /// Current turn number.
        /// </summary>
        private int currentTurn = 0;
        
        /// <summary>
        /// Maximum number of events to keep in memory (prevent memory bloat).
        /// Older events are archived/discarded.
        /// </summary>
        public int maxStoredEvents = 10000;
        
        /// <summary>
        /// Read-only access to all events.
        /// </summary>
        public IReadOnlyList<RaceEvent> AllEvents => allEvents;
        
        /// <summary>
        /// Logs a new event to the history.
        /// </summary>
        public static void LogEvent(RaceEvent raceEvent)
        {
            Instance.LogEventInternal(raceEvent);
        }
        
        /// <summary>
        /// Convenience method for logging simple events.
        /// </summary>
        public static RaceEvent Log(
            EventType type,
            EventImportance importance,
            string description,
            Stage location = null,
            params Vehicle[] vehicles)
        {
            RaceEvent evt = new RaceEvent(
                Instance.currentTurn,
                type,
                importance,
                description,
                location,
                vehicles
            );
            
            Instance.LogEventInternal(evt);

            return evt;
        }
        
        /// <summary>
        /// Internal event logging implementation.
        /// </summary>
        private void LogEventInternal(RaceEvent evt)
        {
            // Ensure turn number is set
            if (evt.turnNumber == 0)
                evt.turnNumber = currentTurn;
            
            allEvents.Add(evt);
            
            // Also log to old SimulationLogger for backwards compatibility
            SimulationLogger.LogEvent(evt.description);
            
            // Index by vehicles
            foreach (var vehicle in evt.involvedVehicles)
            {
                if (vehicle == null) continue;
                
                if (!vehicleEvents.ContainsKey(vehicle))
                {
                    vehicleEvents[vehicle] = new List<RaceEvent>();
                }
                
                vehicleEvents[vehicle].Add(evt);
            }
            
            // Trim if exceeding max
            if (allEvents.Count > maxStoredEvents)
            {
                int toRemove = allEvents.Count - maxStoredEvents;
                allEvents.RemoveRange(0, toRemove);
            }
            
            // Debug log in editor
            #if UNITY_EDITOR
            if (evt.importance <= EventImportance.High)
            {
                Debug.Log($"[RaceHistory T{evt.turnNumber}] {evt.description}");
            }
            #endif
        }
        
        /// <summary>
        /// Advances the turn counter.
        /// </summary>
        public static void AdvanceTurn()
        {
            Instance.currentTurn++;
            Log(EventType.System, EventImportance.Low, $"=== Turn {Instance.currentTurn} ===");
        }
        
        /// <summary>
        /// Gets all events for a specific vehicle.
        /// </summary>
        public static List<RaceEvent> GetVehicleEvents(Vehicle vehicle)
        {
            if (Instance.vehicleEvents.ContainsKey(vehicle))
            {
                return Instance.vehicleEvents[vehicle];
            }
            return new List<RaceEvent>();
        }
        
        /// <summary>
        /// Gets events filtered by importance.
        /// </summary>
        public static List<RaceEvent> GetEventsByImportance(EventImportance minImportance)
        {
            return Instance.allEvents.Where(e => e.importance <= minImportance).ToList();
        }
        
        /// <summary>
        /// Gets events filtered by type.
        /// </summary>
        public static List<RaceEvent> GetEventsByType(EventType type)
        {
            return Instance.allEvents.Where(e => e.type == type).ToList();
        }
        
        /// <summary>
        /// Gets events from a specific turn range.
        /// </summary>
        public static List<RaceEvent> GetEventsInTurnRange(int startTurn, int endTurn)
        {
            return Instance.allEvents
                .Where(e => e.turnNumber >= startTurn && e.turnNumber <= endTurn)
                .ToList();
        }
        
        /// <summary>
        /// Gets highlight-worthy events for post-race reel.
        /// </summary>
        public static List<RaceEvent> GetHighlights(int topN = 10)
        {
            return Instance.allEvents
                .Where(e => e.IsHighlightWorthy)
                .OrderByDescending(e => e.CalculateDramaScore())
                .Take(topN)
                .ToList();
        }
        
        /// <summary>
        /// Generates a narrative summary for a specific vehicle.
        /// </summary>
        public static string GenerateVehicleStory(Vehicle vehicle)
        {
            var events = GetVehicleEvents(vehicle);
            
            if (events.Count == 0)
            {
                return $"{vehicle.vehicleName} had an uneventful race.";
            }

            // Key moments
            var combatEvents = events.Where(e => e.type == EventType.Combat).ToList();
            var hazards = events.Where(e => e.type == EventType.StageHazard).ToList();
            var heroic = events.Where(e => e.type == EventType.HeroicMoment).ToList();
            var tragic = events.Where(e => e.type == EventType.TragicMoment).ToList();
            
            string story = $"<b>{vehicle.vehicleName}'s Race:</b>\n\n";
            
            if (heroic.Count > 0)
            {
                story += $"[HERO] {heroic.Count} heroic moment(s)\n";
            }
            
            if (tragic.Count > 0)
            {
                story += $"[TRAGIC] {tragic.Count} tragic moment(s)\n";
            }
            
            if (combatEvents.Count > 0)
            {
                int totalDamageDealt = combatEvents
                    .Where(e => e.metadata.ContainsKey("damage"))
                    .Sum(e => (int)e.metadata["damage"]);
                    
                story += $"[ATK] {combatEvents.Count} combat engagement(s), {totalDamageDealt} damage dealt\n";
            }
            
            if (hazards.Count > 0)
            {
                story += $"[WARN] {hazards.Count} hazard(s) encountered\n";
            }
            
            // Final outcome
            var finishEvent = events.FirstOrDefault(e => e.type == EventType.FinishLine);
            var destroyedEvent = events.FirstOrDefault(e => e.type == EventType.Destruction);
            
            if (finishEvent != null)
            {
                story += $"\n[OK] Finished the race (Turn {finishEvent.turnNumber})";
            }
            else if (destroyedEvent != null)
            {
                story += $"\n[X] Eliminated (Turn {destroyedEvent.turnNumber})";
            }
            else
            {
                story += $"\n[...] Still racing...";
            }
            
            return story;
        }
        
        /// <summary>
        /// Clears all event history (for new race).
        /// </summary>
        public static void ClearHistory()
        {
            Instance.allEvents.Clear();
            Instance.vehicleEvents.Clear();
            Instance.currentTurn = 0;
            Log(EventType.System, EventImportance.Medium, "Race history cleared");
        }
        
        /// <summary>
        /// Exports event history to JSON string.
        /// </summary>
        public static string ExportToJSON()
        {
            return JsonUtility.ToJson(new EventHistoryData { events = Instance.allEvents }, true);
        }
        
        [System.Serializable]
        private class EventHistoryData
        {
            public List<RaceEvent> events;
        }
    }
}