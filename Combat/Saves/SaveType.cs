namespace Assets.Scripts.Combat.Saves
{
    /// <summary>
    /// Types of saving throws in the game.
    /// Each represents a different defensive capability.
    /// </summary>
    public enum SaveType
    {
        None,      // No saving throw (used for event cards with no save required)
        
        /// <summary>
        /// Dodging, evasion, and reaction speed.
        /// Used for: AOE attacks, traps, environmental hazards.
        /// Influenced by: Chassis design, drive power, weight distribution.
        /// </summary>
        Mobility,
        
        // Future saves (not yet implemented):
        // Shoddiness,  // Structural integrity vs destruction
        // Systems,     // Electronic resistance vs hacking/EMP
        // Stability    // Balance and control vs knockdown
    }
}
