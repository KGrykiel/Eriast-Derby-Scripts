using UnityEngine;
using System.Collections.Generic;
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
        public string shortDescription;
        public Dictionary<string, object> metadata = new();
        public float timestamp;

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
            shortDescription = desc.Length > 50 ? desc[..47] + "..." : desc;
            location = stage;
            timestamp = Time.time;

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
            string color = importance switch
            {
                EventImportance.Critical => "#FF4444",
                EventImportance.High => "#FFAA44",
                EventImportance.Medium => "#FFFFFF",
                EventImportance.Low => "#AAAAAA",
                _ => "#888888"
            };

            string icon = GetTypeIcon();
            string text = $"<color={color}>{icon} {description}</color>";

            if (includeLocation && location != null)
            {
                text += $" <color=#888888>({location.stageName})</color>";
            }

            if (includeTimestamp)
            {
                text += $" <color=#666666>[T{turnNumber}]</color>";
            }

            return text;
        }

        private string GetTypeIcon()
        {
            return type switch
            {
                EventType.Combat => "[ATK]",
                EventType.Movement => "[MOVE]",
                EventType.StageHazard => "[WARN]",
                EventType.Modifier => "[MOD]",
                EventType.StatusEffect => "[STATUS]",
                EventType.SkillUse => "[SKILL]",
                EventType.Destruction => "[DEAD]",
                EventType.FinishLine => "[FINISH]",
                EventType.Rivalry => "[POWER]",
                EventType.HeroicMoment => "[HERO]",
                EventType.TragicMoment => "[TRAGIC]",
                EventType.System => "[SYS]",
                EventType.Resource => "[RES]",
                EventType.EventCard => "[EVENT]",
                _ => "[-]"
            };
        }
    }
}