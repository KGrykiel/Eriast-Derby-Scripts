using System.Collections.Generic;
using System.Linq;

namespace Assets.Scripts.Combat.SkillChecks
{
    /// <summary>
    /// Result of a skill check (d20 + bonuses vs DC).
    /// Built once with complete data by SkillCheckCalculator.
    /// </summary>
    [System.Serializable]
    public class SkillCheckResult : ID20RollResult
    {
        public CheckSpec checkSpec;
        
        public int BaseRoll { get; }
        public List<RollBonus> Bonuses { get; }
        public int TargetValue { get; }
        public bool? Success { get; }
        
        public int Total => BaseRoll + TotalModifier;
        public int TotalModifier => Bonuses?.Sum(b => b.Value) ?? 0;
        public bool Succeeded => Success == true;
        
        public SkillCheckResult(
            int baseRoll,
            CheckSpec checkSpec,
            List<RollBonus> bonuses,
            int targetValue,
            bool success)
        {
            BaseRoll = baseRoll;
            this.checkSpec = checkSpec;
            Bonuses = bonuses ?? new List<RollBonus>();
            TargetValue = targetValue;
            Success = success;
        }
    }
}

