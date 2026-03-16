using System;
using System.Collections.Generic;
using Assets.Scripts.Combat.Rolls.Advantage;

namespace Assets.Scripts.Combat.Rolls
{
    /// <summary>Universal d20 roll mechanics. All d20 rolls in the game are funneled here</summary>
    public static class D20Calculator
    {
        /// <summary>Nat 20 = auto-success (crit), nat 1 = auto-fail (fumble).</summary>
        public static D20RollOutcome Roll(
            List<RollBonus> bonuses,
            int targetValue,
            AdvantageSource[] advantageSources = null)
        {
            RollMode mode = D20RollHelpers.ResolveMode(advantageSources);

            int firstRoll = RollUtility.RollD20();
            int keptRoll = firstRoll;
            int? droppedRoll = null;

            if (mode != RollMode.Normal)
            {
                int secondRoll = RollUtility.RollD20();
                keptRoll = mode == RollMode.Advantage
                    ? Math.Max(firstRoll, secondRoll)
                    : Math.Min(firstRoll, secondRoll);
                droppedRoll = firstRoll == keptRoll ? secondRoll : firstRoll;
            }

            int totalModifier = D20RollHelpers.SumBonuses(bonuses);
            int total = keptRoll + totalModifier;
            bool isCrit = keptRoll == 20;
            bool isFumble = keptRoll == 1;
            bool success = isCrit || (!isFumble && total >= targetValue);

            var advantage = new AdvantageResult(mode, droppedRoll, advantageSources ?? Array.Empty<AdvantageSource>());

            return new D20RollOutcome(
                keptRoll, bonuses, totalModifier, total, targetValue,
                success, isCrit, isFumble, advantage);
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
