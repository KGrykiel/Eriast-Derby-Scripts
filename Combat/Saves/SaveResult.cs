using System.Collections.Generic;
using System.Linq;

namespace Combat.Saves
{
    /// <summary>
    /// Result of a saving throw (d20 + save bonus vs DC).
    /// 
    /// Flow: Target rolls d20 + save bonus vs effect's DC
    /// Success = Total >= DC (target resists the effect)
    /// 
    /// Pure data class:
    /// - Use SaveCalculator for roll logic
    /// - Use CombatLogManager for display formatting
    /// </summary>
    [System.Serializable]
    public class SaveResult
    {
        /// <summary>Type of save made (Mobility, etc.)</summary>
        public SaveType saveType;
        
        /// <summary>The actual d20 result (1-20)</summary>
        public int baseRoll;
        
        /// <summary>Size of die rolled (always 20)</summary>
        public int dieSize = 20;
        
        /// <summary>Number of dice (always 1)</summary>
        public int diceCount = 1;
        
        /// <summary>All bonuses and penalties applied to the save</summary>
        public List<AttributeModifier> modifiers;
        
        /// <summary>DC to beat</summary>
        public int targetValue;
        
        /// <summary>Whether the save succeeded (null if not yet evaluated)</summary>
        public bool? success;
        
        /// <summary>Total roll after all modifiers (baseRoll + sum of modifiers)</summary>
        public int Total => baseRoll + TotalModifier;
        
        /// <summary>Sum of all modifiers (excluding base roll)</summary>
        public int TotalModifier => modifiers?.Sum(m => (int)m.Value) ?? 0;
        
        /// <summary>Convenience property - true if the save succeeded (target resisted)</summary>
        public bool Succeeded => success == true;
        
        public SaveResult()
        {
            modifiers = new List<AttributeModifier>();
            dieSize = 20;
            diceCount = 1;
        }
        
        /// <summary>
        /// Create a save result from a d20 roll.
        /// </summary>
        public static SaveResult FromD20(int baseRoll, SaveType saveType)
        {
            return new SaveResult
            {
                baseRoll = baseRoll,
                dieSize = 20,
                diceCount = 1,
                saveType = saveType,
                modifiers = new List<AttributeModifier>()
            };
        }
    }
}
