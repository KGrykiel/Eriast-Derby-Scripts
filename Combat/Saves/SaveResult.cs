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
        
        /// <summary>
        /// Character who made the save (null for vehicle-only saves).
        /// Stored here so event emission can log "Technician saves" vs "Vehicle saves".
        /// </summary>
        public Character Character;
        
        /// <summary>
        /// True if this was an automatic failure (required component missing).
        /// Auto-fails have baseRoll = 0 and no bonuses.
        /// </summary>
        public bool IsAutoFail { get; }
        
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
            bool success,
            Character character = null,
            bool isAutoFail = false)
        {
            BaseRoll = baseRoll;
            this.saveSpec = saveSpec;
            Bonuses = bonuses ?? new List<RollBonus>();
            TargetValue = targetValue;
            Success = success;
            Character = character;
            IsAutoFail = isAutoFail;
        }
        
        /// <summary>
        /// Create an automatic failure result (required component missing/broken).
        /// </summary>
        public static SaveResult AutoFail(SaveSpec saveSpec, int dc)
        {
            return new SaveResult(
                baseRoll: 0,
                saveSpec: saveSpec,
                bonuses: new List<RollBonus>(),
                targetValue: dc,
                success: false,
                character: null,
                isAutoFail: true);
        }
    }
}

