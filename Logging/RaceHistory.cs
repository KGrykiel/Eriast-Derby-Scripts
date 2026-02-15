using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Stages;

namespace Assets.Scripts.Logging
{
    /// <summary>Central race event log. Round/turn tracking owned by TurnStateMachine.</summary>
    public class RaceHistory : MonoBehaviour
    {
        private static RaceHistory instance;
        public static RaceHistory Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject go = new("RaceHistory");
                    instance = go.AddComponent<RaceHistory>();
                    DontDestroyOnLoad(go);
                }
                return instance;
            }
        }

        private List<RaceEvent> allEvents = new();
        private Dictionary<Vehicle, List<RaceEvent>> vehicleEvents = new();
        public int maxStoredEvents = 10000;

        public IReadOnlyList<RaceEvent> AllEvents => allEvents;
        
        public static RaceEvent Log(
            EventType type,
            EventImportance importance,
            string description,
            Stage location = null,
            params Vehicle[] vehicles)
        {
            int currentRound = GetCurrentRound();

            RaceEvent evt = new(
                currentRound,
                type,
                importance,
                description,
                location,
                vehicles
            );

            Instance.LogEventInternal(evt);

            return evt;
        }

        private static int GetCurrentRound()
        {
            var gameManager = FindFirstObjectByType<GameManager>();
            var stateMachine = gameManager?.GetStateMachine();
            return stateMachine?.CurrentRound ?? 0;
        }

        private void LogEventInternal(RaceEvent evt)
        {
            if (evt.turnNumber == 0)
                evt.turnNumber = GetCurrentRound();

            allEvents.Add(evt);

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
                Debug.Log($"[RaceHistory R{evt.turnNumber}] {evt.description}");
            }
            #endif
        }


        public static List<RaceEvent> GetVehicleEvents(Vehicle vehicle)
        {
            if (Instance.vehicleEvents.ContainsKey(vehicle))
                return Instance.vehicleEvents[vehicle];
            return new List<RaceEvent>();
        }

        public static void ClearHistory()
        {
            Instance.allEvents.Clear();
            Instance.vehicleEvents.Clear();
        }
    }
}