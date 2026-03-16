using System.Collections.Generic;
using Assets.Scripts.Combat.Rolls.Advantage;

namespace Assets.Scripts.Combat.Rolls
{
    /// <summary>
    /// All information related to the outcome of a D20 roll.
    /// </summary>
    public class D20RollOutcome
    {
        public int BaseRoll { get; }
        public List<RollBonus> Bonuses { get; }
        public int TotalModifier { get; }
        public int Total { get; }

        /// <summary>Armor class or difficulty class usually.</summary>
        public int TargetValue { get; }
        public bool Success { get; }
        public bool IsCriticalHit { get; }
        public bool IsFumble { get; }
        public AdvantageResult Advantage { get; }

        public D20RollOutcome(
            int baseRoll,
            List<RollBonus> bonuses,
            int totalModifier,
            int total,
            int targetValue,
            bool success,
            bool isCriticalHit,
            bool isFumble,
            AdvantageResult advantage = default)
        {
            BaseRoll = baseRoll;
            Bonuses = bonuses;
            TotalModifier = totalModifier;
            Total = total;
            TargetValue = targetValue;
            Success = success;
            IsCriticalHit = isCriticalHit;
            IsFumble = isFumble;
            Advantage = advantage;
        }
    }
}
