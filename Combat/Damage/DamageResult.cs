namespace Assets.Scripts.Combat.Damage
{
    /// <summary>
    /// Complete result of a single damage roll.
    /// Pure immutable data - all calculation happens in DamageCalculator.
    /// Follows the same pattern as AttackResult, SaveResult, and SkillCheckResult.
    /// 
    /// One formula = one roll = one result.
    /// Composite damage uses multiple DamageResults, aggregated by CombatEventBus.
    /// </summary>
    [System.Serializable]
    public class DamageResult
    {
        /// <summary>Type of damage (Fire, Physical, etc.)</summary>
        public DamageType DamageType { get; }

        /// <summary>Number of dice rolled</summary>
        public int DiceCount { get; }

        /// <summary>Size of dice (d6, d8, etc.)</summary>
        public int DieSize { get; }

        /// <summary>Flat bonus added to damage</summary>
        public int Bonus { get; }

        /// <summary>Sum of dice rolled (without bonus)</summary>
        public int Rolled { get; }

        /// <summary>Whether this was a critical hit (doubled dice)</summary>
        public bool IsCritical { get; }

        /// <summary>Raw total before resistance (Rolled + Bonus)</summary>
        public int RawTotal { get; }

        /// <summary>Resistance applied to this damage</summary>
        public ResistanceLevel ResistanceLevel { get; }

        /// <summary>Final damage after resistance</summary>
        public int FinalDamage { get; }

        public DamageResult(
            DamageType damageType,
            int diceCount,
            int dieSize,
            int bonus,
            int rolled,
            bool isCritical,
            int rawTotal,
            ResistanceLevel resistanceLevel,
            int finalDamage)
        {
            DamageType = damageType;
            DiceCount = diceCount;
            DieSize = dieSize;
            Bonus = bonus;
            Rolled = rolled;
            IsCritical = isCritical;
            RawTotal = rawTotal;
            ResistanceLevel = resistanceLevel;
            FinalDamage = finalDamage;
        }
    }
}
