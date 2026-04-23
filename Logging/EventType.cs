namespace Assets.Scripts.Logging
{
    public enum EventType
    {
        /// <summary>
        /// Combat-related events: attacks, damage, hits, misses.
        /// </summary>
        Combat,

        /// <summary>
        /// Movement events: stage transitions, progress updates.
        /// </summary>
        Movement,

        /// <summary>
        /// Stage hazards and environmental effects.
        /// </summary>
        StageHazard,

        /// <summary>
        /// Status effect applications (buffs, debuffs, conditions with duration).
        /// Examples: Haste, Burning, Stunned, Blessed.
        /// </summary>
        Condition,

        /// <summary>
        /// Vehicle destruction events.
        /// </summary>
        Destruction,

        /// <summary>
        /// Finish line crossings and race completion.
        /// </summary>
        FinishLine,

        /// <summary>
        /// System messages, turn starts, phase changes, internal engine events.
        /// </summary>
        System,

        /// <summary>
        /// Resource management: energy use, health changes.
        /// </summary>
        Resource,

        /// <summary>
        /// Event card triggers and skill checks.
        /// </summary>
        EventCard,

        /// <summary>
        /// AI pipeline events: perception, command weights, skill selection, idle turns.
        /// Always logged at Debug importance — hidden by default.
        /// </summary>
        AI
    }
}