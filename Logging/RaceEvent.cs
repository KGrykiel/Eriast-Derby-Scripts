using System.Collections.Generic;
using Assets.Scripts.Entities.Vehicles;
using Assets.Scripts.Stages;

namespace Assets.Scripts.Logging
{
    /// <summary>
    /// Represents an event that occurs during a race, such as a combat action, a stage hazard, or a status effect application.
    /// Contains all relevant information for logging and display in the UI including importance for filtering.
    /// </summary>
    [System.Serializable]
    public class RaceEvent
    {
        public int turnNumber;
        public EventType type;
        public EventImportance importance;
        public Stage location;
        public List<Vehicle> involvedVehicles = new();
        public string description;
        public Dictionary<string, object> metadata = new();

        public RaceEvent(
            int turn,
            EventType eventType,
            EventImportance eventImportance,
            string desc,
            Stage stage = null,
            params Vehicle[] vehicles)
        {
            turnNumber = turn;
            type = eventType;
            importance = eventImportance;
            description = desc;
            location = stage;

            if (vehicles != null && vehicles.Length > 0)
            {
                involvedVehicles.AddRange(vehicles);
            }
        }

        public RaceEvent WithMetadata(string key, object value)
        {
            metadata[key] = value;
            return this;
        }

        public string GetFormattedText(bool includeTimestamp = false, bool includeLocation = true)
        {
            string icon = GetTypeIcon();
            string text = $"{icon} {LogColors.FormatImportanceText(importance, description)}";

            if (includeLocation && location != null)
            {
                text += $" <color={LogColors.FeedLocation}>({location.stageName})</color>";
            }

            if (includeTimestamp)
            {
                text += $" <color={LogColors.FeedTimestamp}>[T{turnNumber}]</color>";
            }

            return text;
        }

        private string GetTypeIcon()
        {
            return type switch
            {
                EventType.Combat       => $"<color={LogColors.IconCombat}>[ATK]</color>",
                EventType.Movement     => $"<color={LogColors.IconMovement}>[MOVE]</color>",
                EventType.StageHazard  => $"<color={LogColors.IconStageHazard}>[WARN]</color>",
                EventType.Condition    => $"<color={LogColors.IconCondition}>[STATUS]</color>",
                EventType.Destruction  => $"<color={LogColors.IconDestruction}>[DEAD]</color>",
                EventType.FinishLine   => $"<color={LogColors.IconFinishLine}>[FINISH]</color>",
                EventType.System       => $"<color={LogColors.IconSystem}>[SYS]</color>",
                EventType.Resource     => $"<color={LogColors.IconResource}>[RES]</color>",
                EventType.EventCard    => $"<color={LogColors.IconEventCard}>[EVENT]</color>",
                EventType.AI           => $"<color={LogColors.IconAI}>[AI]</color>",
                _                      => $"<color={LogColors.IconDefault}>[-]</color>"
            };
        }
    }
}