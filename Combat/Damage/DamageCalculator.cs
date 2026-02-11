namespace Assets.Scripts.Combat.Damage
{
    /// <summary>
    /// Calculator for damage rolls.
    /// Takes a DamageFormula (pure data) and computes an immutable DamageResult.
    /// Follows the same pattern as AttackCalculator, SaveCalculator, and SkillCheckCalculator.
    /// 
    /// Weapon-agnostic: weapon resolution happens upstream in DamageEffect.
    /// All dice rolling and damage calculation happens here.
    /// </summary>
    public static class DamageCalculator
    {
        /// <summary>
        /// Compute damage from a formula with resistance.
        /// Critical hits double dice count, not flat bonuses.
        /// </summary>
        /// <param name="formula">Damage formula configuration (pure data)</param>
        /// <param name="resistance">Target's resistance level to this damage type</param>
        /// <param name="isCriticalHit">If true, doubles dice count</param>
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

            int rolled = (diceCount > 0 && formula.dieSize > 0) 
                ? RollUtility.RollDice(diceCount, formula.dieSize) 
                : 0;

            int rawTotal = rolled + formula.bonus;
            int finalDamage = ApplyResistance(rawTotal, resistance);

            return new DamageResult(
                formula.damageType,
                diceCount,
                formula.dieSize,
                formula.bonus,
                rolled,
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
