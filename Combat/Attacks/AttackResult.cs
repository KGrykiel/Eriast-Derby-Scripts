using System.Collections.Generic;
using System.Linq;

namespace Assets.Scripts.Combat.Attacks
{
    /// <summary>
    /// Result of an attack roll (d20 + bonuses vs AC).
    /// Built once with complete data by AttackCalculator.
    /// </summary>
    [System.Serializable]
    public class AttackResult : ID20RollResult
    {
        public int BaseRoll { get; }
        public List<RollBonus> Bonuses { get; }
        public int TargetValue { get; }
        public bool? Success { get; }
        
        public bool IsCriticalHit { get; }
        public bool IsCriticalMiss { get; }
        
        public int Total => BaseRoll + TotalModifier;
        public int TotalModifier => Bonuses?.Sum(b => b.Value) ?? 0;
        
        public AttackResult(
            int baseRoll,
            List<RollBonus> bonuses,
            int targetValue,
            bool success,
            bool isCriticalHit = false,
            bool isCriticalMiss = false)
        {
            BaseRoll = baseRoll;
            Bonuses = bonuses ?? new List<RollBonus>();
            TargetValue = targetValue;
            Success = success;
            IsCriticalHit = isCriticalHit;
            IsCriticalMiss = isCriticalMiss;
        }
    }
}
