using System;
using System.Collections.Generic;
using Assets.Scripts.Combat.Rolls.Advantage;

namespace Assets.Scripts.Combat.Rolls
{
    /// <summary>Universal d20 roll mechanics. All d20 rolls in the game are funneled here</summary>
    public static class D20Calculator
    {
        /// <summary>Nat 20 = crit, nat 1 = fumble. Success evaluation belongs in each performer.</summary>
        public static D20RollData Roll(GatheredRoll gatheredRoll)
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

            var advantage = new AdvantageResult(mode, droppedRoll, gatheredRoll.AdvantageSources);

            return new D20RollData(keptRoll, gatheredRoll.Bonuses, totalModifier, total, isCrit, isFumble, advantage);
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
