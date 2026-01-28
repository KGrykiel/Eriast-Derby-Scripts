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
        int BaseRoll { get; set; }
        
        /// <summary>Size of die rolled (always 20 for d20 rolls)</summary>
        int DieSize { get; set; }
        
        /// <summary>Number of dice (always 1 for standard d20 rolls)</summary>
        int DiceCount { get; set; }
        
        /// <summary>All bonuses and penalties applied to the roll</summary>
        List<AttributeModifier> Modifiers { get; set; }
        
        /// <summary>Target number to beat (AC, DC, etc.)</summary>
        int TargetValue { get; set; }
        
        /// <summary>Whether the roll succeeded (null if not yet evaluated)</summary>
        bool? Success { get; set; }
        
        /// <summary>Total roll after all modifiers (baseRoll + sum of modifiers)</summary>
        int Total { get; }
        
        /// <summary>Sum of all modifiers (excluding base roll)</summary>
        int TotalModifier { get; }
    }
}
