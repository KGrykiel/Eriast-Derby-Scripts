namespace Assets.Scripts.Combat
{
    /// <summary>
    /// Shared non-D20 dice roller. The single place NdX+bonus is evaluated.
    /// Same role as D20Calculator for D20 rolls.
    /// </summary>
    public static class DiceCalculator
    {
        /// <summary>
        /// Roll baseDice dice of dieSize sides, then add bonus.
        /// If baseDice is 0, returns bonus only (flat amount, no rolling).
        /// </summary>
        public static int Roll(int baseDice, int dieSize, int bonus)
        {
            int rolled = (baseDice > 0 && dieSize > 0)
                ? RollUtility.RollDice(baseDice, dieSize)
                : 0;

            return rolled + bonus;
        }
    }
}
