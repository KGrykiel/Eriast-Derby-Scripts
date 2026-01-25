using Assets.Scripts.Skills.Helpers;

namespace Assets.Scripts.Effects
{
    /// <summary>
    /// Combat/situational state passed to effects during execution.
    /// Kept separate from SkillContext because effects can come from non-skill sources
    /// (environmental damage, status effect ticks, event cards, etc.)
    /// 
    /// Design: Narrow interface for IEffect - only contains state effects actually need.
    /// </summary>
    public struct EffectContext
    {
        // ===== COMBAT STATE =====
        
        /// <summary>
        /// Natural 20 on attack roll - doubles damage dice (not flat bonuses).
        /// </summary>
        public bool IsCriticalHit;
        
        // Future fields:
        // public int LanePosition;      // For movement effects
        // public float WeatherModifier; // Environmental bonuses
        // public int ComboCount;        // Combo system multipliers
        
        // ===== FACTORY METHODS =====
        
        /// <summary>
        /// Default context with no special state. Use for non-combat effects.
        /// </summary>
        public static EffectContext Default => new EffectContext();
        
        /// <summary>
        /// Create EffectContext from SkillContext (translation point).
        /// </summary>
        public static EffectContext FromSkillContext(SkillContext ctx)
        {
            return new EffectContext
            {
                IsCriticalHit = ctx.IsCriticalHit
            };
        }
    }
}
