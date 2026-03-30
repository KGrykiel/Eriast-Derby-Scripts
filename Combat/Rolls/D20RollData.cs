using System.Collections.Generic;
using Assets.Scripts.Combat.Rolls.Advantage;

namespace Assets.Scripts.Combat.Rolls
{
    /// <summary>
    /// Mechanical facts produced by D20Calculator.Roll — dice result, bonuses, and nat flags.
    /// Transient construction artifact; performers evaluate success and construct D20RollOutcome.
    /// </summary>
    public readonly struct D20RollData
    {
        public readonly int KeptRoll;
        public readonly List<RollBonus> Bonuses;
        public readonly int TotalModifier;
        public readonly int Total;
        public readonly bool IsCrit;
        public readonly bool IsFumble;
        public readonly AdvantageResult Advantage;

        public D20RollData(
            int keptRoll,
            List<RollBonus> bonuses,
            int totalModifier,
            int total,
            bool isCrit,
            bool isFumble,
            AdvantageResult advantage)
        {
            KeptRoll = keptRoll;
            Bonuses = bonuses;
            TotalModifier = totalModifier;
            Total = total;
            IsCrit = isCrit;
            IsFumble = isFumble;
            Advantage = advantage;
        }
    }
}
