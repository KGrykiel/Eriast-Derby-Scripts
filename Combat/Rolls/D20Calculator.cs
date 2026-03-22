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
            GatheredRoll gatheredRoll,
            int targetValue)
        {
            RollMode mode = ResolveMode(gatheredRoll.AdvantageSources);

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

            int totalModifier = SumBonuses(gatheredRoll.Bonuses);
            int total = keptRoll + totalModifier;
            bool isCrit = keptRoll == 20;
            bool isFumble = keptRoll == 1;
            bool success = isCrit || (!isFumble && total >= targetValue);

            var advantage = new AdvantageResult(mode, droppedRoll, gatheredRoll.AdvantageSources);

            return new D20RollOutcome(
                keptRoll, gatheredRoll.Bonuses, totalModifier, total, targetValue,
                success, isCrit, isFumble, advantage);
        }

        /// <summary>Synthetic failed roll — e.g. no suitable character found to attempt.</summary>
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
                isFumble: false,
                isAutoFail: true);
        }

        /// <summary>Synthetic successful roll — e.g. the opposing side cannot attempt.</summary>
        public static D20RollOutcome AutoSuccess(int targetValue)
        {
            return new D20RollOutcome(
                baseRoll: 0,
                bonuses: new List<RollBonus>(),
                totalModifier: 0,
                total: 0,
                targetValue: targetValue,
                success: true,
                isCriticalHit: false,
                isFumble: false);
        }

        private static int SumBonuses(List<RollBonus> bonuses)
        {
            int sum = 0;
            foreach (var b in bonuses) sum += b.Value;
            return sum;
        }

        private static RollMode ResolveMode(List<AdvantageSource> sources)
        {
            if (sources == null || sources.Count == 0) return RollMode.Normal;

            bool hasAdvantage = false;
            bool hasDisadvantage = false;
            foreach (var src in sources)
            {
                if (src.Type == RollMode.Advantage) hasAdvantage = true;
                else if (src.Type == RollMode.Disadvantage) hasDisadvantage = true;
            }

            if (hasAdvantage && !hasDisadvantage) return RollMode.Advantage;
            if (hasDisadvantage && !hasAdvantage) return RollMode.Disadvantage;
            return RollMode.Normal;
        }
    }
}
