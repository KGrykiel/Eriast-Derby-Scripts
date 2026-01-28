using System.Collections.Generic;
using System.Linq;

namespace Assets.Scripts.Combat.Attacks
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
        public int BaseRoll { get; set; }
        
        /// <summary>Size of die rolled (always 20)</summary>
        public int DieSize { get; set; } = 20;
        
        /// <summary>Number of dice (always 1)</summary>
        public int DiceCount { get; set; } = 1;
        
        /// <summary>All bonuses and penalties applied to the attack</summary>
        public List<AttributeModifier> Modifiers { get; set; }
        
        /// <summary>Target AC to beat</summary>
        public int TargetValue { get; set; }
        
        /// <summary>Whether the attack hit (null if not yet evaluated)</summary>
        public bool? Success { get; set; }
        
        /// <summary>Whether this is a critical hit (natural 20, auto-hit, double dice)</summary>
        public bool IsCriticalHit { get; set; }
        
        /// <summary>Whether this is a critical miss (natural 1, auto-miss)</summary>
        public bool IsCriticalMiss { get; set; }
        
        /// <summary>Total roll after all modifiers (baseRoll + sum of modifiers)</summary>
        public int Total => BaseRoll + TotalModifier;
        
        /// <summary>Sum of all modifiers (excluding base roll)</summary>
        public int TotalModifier => Modifiers?.Sum(m => (int)m.Value) ?? 0;
        
        public AttackResult()
        {
            Modifiers = new List<AttributeModifier>();
            DieSize = 20;
            DiceCount = 1;
        }
        
        /// <summary>
        /// Create an attack result from a d20 roll.
        /// </summary>
        public static AttackResult FromD20(int baseRoll)
        {
            return new AttackResult
            {
                BaseRoll = baseRoll,
                DieSize = 20,
                DiceCount = 1,
                Modifiers = new List<AttributeModifier>()
            };
        }
    }
}
