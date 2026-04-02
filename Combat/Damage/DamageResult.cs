namespace Assets.Scripts.Combat.Damage
{
    /// <summary>
    /// Immutable result of a single damage roll. One formula = one roll = one result.
    /// Composite damage (e.g. physical + fire) uses multiple DamageResults, aggregated by CombatEventBus.
    /// </summary>
    public readonly struct DamageResult
    {
        public DamageType DamageType { get; }
        public int DiceCount { get; }
        public int DieSize { get; }
        public int Bonus { get; }
        public bool IsCritical { get; }
        public int RawTotal { get; }
        public ResistanceLevel ResistanceLevel { get; }
        public int FinalDamage { get; }

        public DamageResult(
            DamageType damageType,
            int diceCount,
            int dieSize,
            int bonus,
            bool isCritical,
            int rawTotal,
            ResistanceLevel resistanceLevel,
            int finalDamage)
        {
            DamageType = damageType;
            DiceCount = diceCount;
            DieSize = dieSize;
            Bonus = bonus;
            IsCritical = isCritical;
            RawTotal = rawTotal;
            ResistanceLevel = resistanceLevel;
            FinalDamage = finalDamage;
        }
    }
}
