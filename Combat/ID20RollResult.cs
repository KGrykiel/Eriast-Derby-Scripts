using System.Collections.Generic;

namespace Assets.Scripts.Combat
{
    /// <summary>
    /// Interface for all d20 roll result types (attacks, saves, skill checks, etc.).
    /// Provides common properties and calculations for d20-based rolls.
    /// </summary>
    public interface ID20RollResult
    {
        /// <summary>The actual d20 result (1-20)</summary>
        int BaseRoll { get; }
        
        /// <summary>Labeled bonuses/penalties applied to the roll (for tooltip display)</summary>
        List<RollBonus> Bonuses { get; }
        
        /// <summary>Target number to beat (AC, DC, etc.)</summary>
        int TargetValue { get; }
        
        /// <summary>Whether the roll succeeded (null if not yet evaluated)</summary>
        bool? Success { get; }
        
        /// <summary>Total roll after all bonuses (baseRoll + sum of bonuses)</summary>
        int Total { get; }
        
        /// <summary>Sum of all bonuses (excluding base roll)</summary>
        int TotalModifier { get; }
    }
}
