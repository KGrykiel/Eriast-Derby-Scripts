using System.Collections.Generic;

namespace Assets.Scripts.Combat
{
    /// <summary>Universal d20 roll mechanics. All d20 rolls in the game are funneled here</summary>
    public static class D20Calculator
    {
        /// <summary>Nat 20 = auto-success (crit), nat 1 = auto-fail (fumble).</summary>
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

        /// <summary>Constructor for auto-failed rolls e.g. when suitable character not found</summary>
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
