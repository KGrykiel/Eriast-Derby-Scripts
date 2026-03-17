namespace Assets.Scripts.Combat.Damage
{
    /// <summary>The single place in the codebase calculating damage. Might be expanded to include additional mechanics like hardness in the future.</summary>
    public static class DamageCalculator
    {
        /// <summary>Critical hits double dice count only, not flat bonuses. According to DnD rules.</summary>
        public static DamageResult Compute(
            DamageFormula formula,
            ResistanceLevel resistance,
            bool isCriticalHit = false)
        {
            int diceCount = formula.baseDice;
            bool isCritical = false;

            if (isCriticalHit && diceCount > 0)
            {
                diceCount *= 2;
                isCritical = true;
            }

            int rawTotal = DiceCalculator.Roll(diceCount, formula.dieSize, formula.bonus);
            int finalDamage = ApplyResistance(rawTotal, resistance);

            return new DamageResult(
                formula.damageType,
                diceCount,
                formula.dieSize,
                formula.bonus,
                isCritical,
                rawTotal,
                resistance,
                finalDamage);
        }

        private static int ApplyResistance(int damage, ResistanceLevel resistance)
        {
            return resistance switch
            {
                ResistanceLevel.Vulnerable => damage * 2,
                ResistanceLevel.Resistant => damage / 2,
                ResistanceLevel.Immune => 0,
                _ => damage
            };
        }
    }
}
