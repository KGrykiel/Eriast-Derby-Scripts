namespace Assets.Scripts.Effects
{
    /// <summary>
    /// Universal context for effect execution.
    /// Contains situational/environmental data that effects may need.
    /// All fields are optional - callers populate only what's relevant.
    /// 
    /// Used by all effect sources: Skills, EventCards, StatusEffects, Environmental hazards, etc.
    /// </summary>
    public struct EffectContext
    {
        // ===== COMBAT STATE =====
        
        /// <summary>
        /// Natural 20 on attack roll - doubles damage dice (not flat bonuses).
        /// </summary>
        public bool isCriticalHit;
        
        /// <summary>
        /// Natural 1 on attack roll - automatic miss (already handled before effects apply).
        /// </summary>
        public bool isCriticalMiss;
    }
}
