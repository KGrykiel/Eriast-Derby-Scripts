using UnityEngine;
using System.Collections.Generic;

namespace RacingGame.Events
{
    /// <summary>
    /// Represents a single event that occurred during the race.
    /// Stores all data needed for display, filtering, and highlight reel generation.
    /// </summary>
    [System.Serializable]
    public class RaceEvent
    {
        /// <summary>
        /// Turn number when this event occurred.
        /// </summary>
        public int turnNumber;

        /// <summary>
        /// Type of event for categorization and filtering.
        /// </summary>
        public EventType type;

        /// <summary>
        /// Importance level for display prioritization.
        /// </summary>
        public EventImportance importance;

        /// <summary>
        /// Stage where the event occurred (can be null for global events).
        /// </summary>
        public Stage location;

        /// <summary>
        /// Vehicles involved in this event (attacker, target, witnesses, etc.).
        /// </summary>
        public List<Vehicle> involvedVehicles = new List<Vehicle>();

        /// <summary>
        /// Human-readable description of the event for DM narration.
        /// </summary>
        public string description;

        /// <summary>
        /// Short summary for condensed displays (max ~50 chars).
        /// </summary>
        public string shortDescription;

        /// <summary>
        /// Additional metadata for specific event types.
        /// Examples: damage dealt, roll results, modifier values.
        /// </summary>
        public Dictionary<string, object> metadata = new Dictionary<string, object>();

        /// <summary>
        /// Timestamp when event occurred (for debugging/replay).
        /// </summary>
        public float timestamp;

        /// <summary>
        /// Whether this event is worthy of appearing in highlight reel.
        /// </summary>
        public bool IsHighlightWorthy => importance <= EventImportance.High || CalculateDramaScore() > 5f;

        /// <summary>
        /// Calculates how "dramatic" this event is for highlight reel prioritization.
        /// Higher score = more interesting for players to see.
        /// </summary>
        public float CalculateDramaScore()
        {
            float score = 0f;

            // Base score by importance
            score += (int)importance switch
            {
                0 => 10f, // Critical
                1 => 5f,  // High
                2 => 2f,  // Medium
                _ => 0f
            };

            // Type bonuses
            switch (type)
            {
                case EventType.Destruction:
                    score += 8f;
                    break;
                case EventType.HeroicMoment:
                case EventType.TragicMoment:
                    score += 7f;
                    break;
                case EventType.Combat:
                    // Big damage = more dramatic
                    if (metadata.ContainsKey("damage") && metadata["damage"] is int damage)
                    {
                        score += damage / 10f;
                    }
                    break;
                case EventType.FinishLine:
                    score += 6f;
                    break;
            }

            // Multiple vehicles = more interesting
            score += involvedVehicles.Count * 0.5f;

            return score;
        }

        /// <summary>
        /// Constructor for creating events.
        /// </summary>
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
            shortDescription = desc.Length > 50 ? desc.Substring(0, 47) + "..." : desc;
            location = stage;
            timestamp = Time.time;

            if (vehicles != null && vehicles.Length > 0)
            {
                involvedVehicles.AddRange(vehicles);
            }
        }

        /// <summary>
        /// Fluent API for adding metadata.
        /// </summary>
        public RaceEvent WithMetadata(string key, object value)
        {
            metadata[key] = value;
            return this;
        }

        /// <summary>
        /// Fluent API for setting short description.
        /// </summary>
        public RaceEvent WithShortDescription(string shortDesc)
        {
            shortDescription = shortDesc;
            return this;
        }

        /// <summary>
        /// Gets formatted display text with color coding based on importance.
        /// </summary>
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

        /// <summary>
        /// Gets an icon/emoji representing the event type.
        /// </summary>
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