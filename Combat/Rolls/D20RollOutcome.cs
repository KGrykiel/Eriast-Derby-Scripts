using System.Collections.Generic;
using Assets.Scripts.Combat.Rolls.Advantage;

namespace Assets.Scripts.Combat.Rolls
{
    /// <summary>
    /// All information related to the outcome of a D20 roll.
    /// </summary>
    public readonly struct D20RollOutcome
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

        /// <summary>True when the roll was never made — no eligible actor existed to attempt it.</summary>
        public bool IsAutoFail { get; }

        /// <summary>True when success was granted without rolling — e.g. the opposing side cannot attempt.</summary>
        public bool IsAutoSuccess { get; }

        public D20RollOutcome(
            int baseRoll,
            List<RollBonus> bonuses,
            int totalModifier,
            int total,
            int targetValue,
            bool success,
            bool isCriticalHit,
            bool isFumble,
            AdvantageResult advantage = default,
            bool isAutoFail = false,
            bool isAutoSuccess = false)
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
            IsAutoFail = isAutoFail;
            IsAutoSuccess = isAutoSuccess;
        }

        /// <summary>Synthetic failed roll — e.g. no suitable character found to attempt.</summary>
        public static D20RollOutcome AutoFail(int targetValue) => new(
            baseRoll: 0,
            bonuses: new List<RollBonus>(),
            totalModifier: 0,
            total: 0,
            targetValue: targetValue,
            success: false,
            isCriticalHit: false,
            isFumble: false,
            isAutoFail: true);

        /// <summary>Synthetic successful roll — e.g. the opposing side cannot attempt.</summary>
        public static D20RollOutcome AutoSuccess(int targetValue) => new(
            baseRoll: 0,
            bonuses: new List<RollBonus>(),
            totalModifier: 0,
            total: 0,
            targetValue: targetValue,
            success: true,
            isCriticalHit: false,
            isFumble: false,
            isAutoSuccess: true);
    }
}
