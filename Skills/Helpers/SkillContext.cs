namespace Skills.Helpers
{
    /// <summary>
    /// Provides situational/environmental context for skill effect execution.
    /// Contains combat state (crits, flanking) and environmental modifiers (stage effects).
    /// Passed to effects via the context parameter for effects that need this information.
    /// </summary>
    public struct SkillContext
    {
        /// <summary>
        /// Whether this skill execution is a critical hit (natural 20 on attack roll).
        /// Damages should double weapon dice (not flat bonuses).
        /// </summary>
        public bool isCriticalHit;
        
        // Future fields:
        // public Stage currentStage;     // For stage-specific modifiers (fire amplification, etc.)
        // public bool isFlanking;        // Positional advantage
        // public bool isSurprised;       // Surprise round
        // public bool isSneak;           // Sneak attack
    }
}
