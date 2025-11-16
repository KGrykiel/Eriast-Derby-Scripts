namespace RacingGame.Events
{
    /// <summary>
    /// Defines importance levels for race events.
    /// Used for filtering and prioritizing what the DM sees.
    /// </summary>
    public enum EventImportance
    {
        /// <summary>
        /// Critical events that must always be shown to DM.
        /// Examples: Vehicle destroyed, finish line crossed, player critically wounded.
        /// </summary>
        Critical = 0,
        
        /// <summary>
        /// Important events that should be shown by default.
        /// Examples: Combat hits, stage hazards triggered, major position changes.
        /// </summary>
        High = 1,
        
        /// <summary>
        /// Standard gameplay events.
        /// Examples: Skill uses, stage transitions, modifier applications.
        /// </summary>
        Medium = 2,
        
        /// <summary>
        /// Minor events that can be hidden by default.
        /// Examples: Movement updates, modifier ticks, minor progress updates.
        /// </summary>
        Low = 3,
        
        /// <summary>
        /// Debug-level events for development only.
        /// Examples: Internal state changes, calculation details, system messages.
        /// </summary>
        Debug = 4
    }
}