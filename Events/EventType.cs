namespace RacingGame.Events
{
    /// <summary>
    /// Categorizes events by type for filtering and visualization.
    /// Each type can have different icons, colors, and display rules.
    /// </summary>
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
        /// Modifier applications, expirations, and effects.
        /// NOTE: This is for PERMANENT modifiers. For status effects with duration, use StatusEffect.
        /// </summary>
        Modifier,
        
        /// <summary>
        /// Status effect applications (buffs, debuffs, conditions with duration).
        /// Examples: Haste, Burning, Stunned, Blessed.
        /// </summary>
        StatusEffect,
        
        /// <summary>
        /// Skill usage (separate from combat for non-attack skills).
        /// </summary>
        SkillUse,
        
        /// <summary>
        /// Vehicle destruction events.
        /// </summary>
        Destruction,
        
        /// <summary>
        /// Finish line crossings and race completion.
        /// </summary>
        FinishLine,
        
        /// <summary>
        /// Repeated interactions between same vehicles (tracked for narrative).
        /// </summary>
        Rivalry,
        
        /// <summary>
        /// Dramatic moments: comebacks, last-stand survivals, clutch plays.
        /// </summary>
        HeroicMoment,
        
        /// <summary>
        /// Dramatic failures: leader destroyed, last-minute defeat.
        /// </summary>
        TragicMoment,
        
        /// <summary>
        /// System messages, turn starts, phase changes.
        /// </summary>
        System,
        
        /// <summary>
        /// Resource management: energy use, health changes.
        /// </summary>
        Resource,
        
        /// <summary>
        /// Event card triggers and skill checks.
        /// </summary>
        EventCard
    }
}