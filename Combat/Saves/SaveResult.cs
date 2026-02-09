using System.Collections.Generic;
using System.Linq;

namespace Assets.Scripts.Combat.Saves
{
    /// <summary>
    /// Result of a saving throw (d20 + bonuses vs DC).
    /// Built once with complete data by SaveCalculator.
    /// </summary>
    [System.Serializable]
    public class SaveResult : ID20RollResult
    {
        public SaveSpec saveSpec;
        
        public int BaseRoll { get; }
        public List<RollBonus> Bonuses { get; }
        public int TargetValue { get; }
        public bool? Success { get; }
        
        public int Total => BaseRoll + TotalModifier;
        public int TotalModifier => Bonuses?.Sum(b => b.Value) ?? 0;
        public bool Succeeded => Success == true;
        
        public SaveResult(
            int baseRoll,
            SaveSpec saveSpec,
            List<RollBonus> bonuses,
            int targetValue,
            bool success)
        {
            BaseRoll = baseRoll;
            this.saveSpec = saveSpec;
            Bonuses = bonuses ?? new List<RollBonus>();
            TargetValue = targetValue;
            Success = success;
        }
    }
}

