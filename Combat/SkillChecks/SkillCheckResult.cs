using System.Collections.Generic;
using System.Linq;

namespace Assets.Scripts.Combat.SkillChecks
{
    /// <summary>
    /// Result of a skill check (d20 + skill bonus vs DC).
    /// 
    /// Flow: Character rolls d20 + skill bonus vs challenge DC
    /// Success = Total >= DC (character succeeds at the task)
    /// 
    /// Pure data class:
    /// - Use SkillCheckCalculator for roll logic
    /// - Use CombatLogManager for display formatting
    /// </summary>
    [System.Serializable]
    public class SkillCheckResult : ID20RollResult
    {
        /// <summary>Type of skill being checked (Pilot, Perception, etc.)</summary>
        public SkillCheckType checkType;
        
        /// <summary>The actual d20 result (1-20)</summary>
        public int baseRoll { get; set; }
        
        /// <summary>Size of die rolled (always 20)</summary>
        public int dieSize { get; set; } = 20;
        
        /// <summary>Number of dice (always 1)</summary>
        public int diceCount { get; set; } = 1;
        
        /// <summary>All bonuses and penalties applied to the check</summary>
        public List<AttributeModifier> modifiers { get; set; }
        
        /// <summary>DC to beat</summary>
        public int targetValue { get; set; }
        
        /// <summary>Whether the check succeeded (null if not yet evaluated)</summary>
        public bool? success { get; set; }
        
        /// <summary>Total roll after all modifiers (baseRoll + sum of modifiers)</summary>
        public int Total => baseRoll + TotalModifier;
        
        /// <summary>Sum of all modifiers (excluding base roll)</summary>
        public int TotalModifier => modifiers?.Sum(m => (int)m.Value) ?? 0;
        
        /// <summary>Convenience property - true if the check succeeded</summary>
        public bool Succeeded => success == true;
        
        public SkillCheckResult()
        {
            modifiers = new List<AttributeModifier>();
            dieSize = 20;
            diceCount = 1;
        }
        
        /// <summary>
        /// Create a skill check result from a d20 roll.
        /// </summary>
        public static SkillCheckResult FromD20(int baseRoll, SkillCheckType checkType)
        {
            return new SkillCheckResult
            {
                baseRoll = baseRoll,
                dieSize = 20,
                diceCount = 1,
                checkType = checkType,
                modifiers = new List<AttributeModifier>()
            };
        }
    }
}

