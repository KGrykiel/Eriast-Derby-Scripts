using System.Collections.Generic;
using System.Linq;

namespace Combat.Attacks
{
    /// <summary>
    /// Result of an attack roll (d20 + attack bonus vs AC).
    /// 
    /// Flow: Attacker rolls d20 + attack bonus vs target's AC
    /// Success = Total >= AC (attacker hits)
    /// 
    /// Pure data class:
    /// - Use AttackCalculator for roll logic
    /// - Use CombatLogManager for display formatting
    /// </summary>
    [System.Serializable]
    public class AttackResult : ID20RollResult
    {
        /// <summary>The actual d20 result (1-20)</summary>
        public int baseRoll { get; set; }
        
        /// <summary>Size of die rolled (always 20)</summary>
        public int dieSize { get; set; } = 20;
        
        /// <summary>Number of dice (always 1)</summary>
        public int diceCount { get; set; } = 1;
        
        /// <summary>All bonuses and penalties applied to the attack</summary>
        public List<AttributeModifier> modifiers { get; set; }
        
        /// <summary>Target AC to beat</summary>
        public int targetValue { get; set; }
        
        /// <summary>Whether the attack hit (null if not yet evaluated)</summary>
        public bool? success { get; set; }
        
        /// <summary>Total roll after all modifiers (baseRoll + sum of modifiers)</summary>
        public int Total => baseRoll + TotalModifier;
        
        /// <summary>Sum of all modifiers (excluding base roll)</summary>
        public int TotalModifier => modifiers?.Sum(m => (int)m.Value) ?? 0;
        
        public AttackResult()
        {
            modifiers = new List<AttributeModifier>();
            dieSize = 20;
            diceCount = 1;
        }
        
        /// <summary>
        /// Create an attack result from a d20 roll.
        /// </summary>
        public static AttackResult FromD20(int baseRoll)
        {
            return new AttackResult
            {
                baseRoll = baseRoll,
                dieSize = 20,
                diceCount = 1,
                modifiers = new List<AttributeModifier>()
            };
        }
    }
}
