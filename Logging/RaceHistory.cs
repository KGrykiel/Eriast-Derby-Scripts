using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Stages;
using Assets.Scripts.Entities.Vehicles;

namespace Assets.Scripts.Logging
{
    /// <summary>Central race event log. Round/turn tracking owned by TurnStateMachine.</summary>
    public class RaceHistory
    {
        private static RaceHistory instance;
        private static RaceHistory Instance => instance ??= new RaceHistory();

        private GameManager gameManager;

        private List<RaceEvent> allEvents = new();
        public int maxStoredEvents = 10000;

        public static IReadOnlyList<RaceEvent> AllEvents => Instance.allEvents;

        public static void Initialize(GameManager gm)
        {
            instance = new RaceHistory();
            instance.gameManager = gm;
        }

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
            if (Instance.gameManager == null) return 0;
            var stateMachine = Instance.gameManager.GetStateMachine();
            return stateMachine?.CurrentRound ?? 0;
        }

        private void LogEventInternal(RaceEvent evt)
        {
            if (evt.turnNumber == 0)
                evt.turnNumber = GetCurrentRound();

            allEvents.Add(evt);

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
                //Debug.Log($"[RaceHistory R{evt.turnNumber}] {evt.description}");
            }
            #endif
        }


        public static List<RaceEvent> GetVehicleEvents(Vehicle vehicle)
            => Instance.allEvents.Where(e => e.involvedVehicles.Contains(vehicle)).ToList();

        public static void ClearHistory()
        {
            Instance.allEvents.Clear();
        }
    }
}