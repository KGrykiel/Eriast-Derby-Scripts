using System.Collections.Generic;

namespace Assets.Scripts.Combat
{
    /// <summary>
    /// Raw d20 roll outcome. Pure data — zero calculations.
    /// All values are pre-computed by D20Calculator.
    /// 
    /// Domain results (SaveResult, SkillCheckResult, AttackResult) wrap this
    /// and add domain-specific context (spec, character, etc.).
    /// </summary>
    public class D20RollOutcome
    {
        /// <summary>The raw d20 result (1-20)</summary>
        public int BaseRoll { get; }

        /// <summary>Labeled bonuses/penalties applied to the roll</summary>
        public List<RollBonus> Bonuses { get; }

        /// <summary>Sum of all bonuses (excluding base roll)</summary>
        public int TotalModifier { get; }

        /// <summary>Total roll (BaseRoll + TotalModifier)</summary>
        public int Total { get; }

        /// <summary>Target number to beat (AC, DC, etc.)</summary>
        public int TargetValue { get; }

        /// <summary>Whether the roll succeeded</summary>
        public bool Success { get; }

        /// <summary>Natural 20 on the die</summary>
        public bool IsCriticalHit { get; }

        /// <summary>Natural 1 on the die</summary>
        public bool IsFumble { get; }

        public D20RollOutcome(
            int baseRoll,
            List<RollBonus> bonuses,
            int totalModifier,
            int total,
            int targetValue,
            bool success,
            bool isCriticalHit,
            bool isFumble)
        {
            BaseRoll = baseRoll;
            Bonuses = bonuses;
            TotalModifier = totalModifier;
            Total = total;
            TargetValue = targetValue;
            Success = success;
            IsCriticalHit = isCriticalHit;
            IsFumble = isFumble;
        }
    }
}
