using System.Collections.Generic;

namespace Assets.Scripts.Combat
{
    /// <summary>
    /// Universal d20 roll calculator. Pure mechanics — no domain knowledge.
    /// All d20 rolls in the game funnel through here.
    /// 
    /// Domain calculators (SaveCalculator, SkillCheckCalculator, AttackCalculator)
    /// gather bonuses according to game rules, then call this to roll.
    /// </summary>
    public static class D20Calculator
    {
        /// <summary>
        /// Roll d20 + bonuses vs target value.
        /// Detects natural 20 (crit) and natural 1 (fumble).
        /// Callers decide whether crit/fumble info is relevant to their domain.
        /// </summary>
        public static D20RollOutcome Roll(List<RollBonus> bonuses, int targetValue)
        {
            int baseRoll = RollUtility.RollD20();
            int totalModifier = D20RollHelpers.SumBonuses(bonuses);
            int total = baseRoll + totalModifier;

            bool isCrit = baseRoll == 20;
            bool isFumble = baseRoll == 1;
            bool success = isCrit || (!isFumble && total >= targetValue);

            return new D20RollOutcome(
                baseRoll,
                bonuses,
                totalModifier,
                total,
                targetValue,
                success,
                isCrit,
                isFumble);
        }

        /// <summary>
        /// Create an automatic failure outcome (no roll occurred).
        /// Used when a check/save can't be attempted (missing component, etc.).
        /// </summary>
        public static D20RollOutcome AutoFail(int targetValue)
        {
            return new D20RollOutcome(
                baseRoll: 0,
                bonuses: new List<RollBonus>(),
                totalModifier: 0,
                total: 0,
                targetValue: targetValue,
                success: false,
                isCriticalHit: false,
                isFumble: false);
        }
    }
}
