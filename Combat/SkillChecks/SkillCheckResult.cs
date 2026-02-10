using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Characters;

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
        
        /// <summary>
        /// Character who made the check (null for vehicle-only checks).
        /// Stored here so event emission can log "Pilot makes Piloting check" vs "Vehicle makes Stability check".
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
        
        public SkillCheckResult(
            int baseRoll,
            CheckSpec checkSpec,
            List<RollBonus> bonuses,
            int targetValue,
            bool success,
            Character character = null,
            bool isAutoFail = false)
        {
            BaseRoll = baseRoll;
            this.checkSpec = checkSpec;
            Bonuses = bonuses ?? new List<RollBonus>();
            TargetValue = targetValue;
            Success = success;
            Character = character;
            IsAutoFail = isAutoFail;
        }
        
        /// <summary>
        /// Create an automatic failure result (required component missing/broken).
        /// </summary>
        public static SkillCheckResult AutoFail(CheckSpec checkSpec, int dc)
        {
            return new SkillCheckResult(
                baseRoll: 0,
                checkSpec: checkSpec,
                bonuses: new List<RollBonus>(),
                targetValue: dc,
                success: false,
                character: null,
                isAutoFail: true);
        }
    }
}