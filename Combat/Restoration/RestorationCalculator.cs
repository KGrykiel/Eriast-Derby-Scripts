using UnityEngine;

namespace Assets.Scripts.Combat.Restoration
{
    /// <summary>
    /// Sole evaluator of RestorationFormula. Same role as DamageCalculator for damage.
    /// Delegates dice rolling to DiceCalculator.
    /// </summary>
    public static class RestorationCalculator
    {
        public static int Roll(RestorationFormula formula)
        {
            if (formula.isDrain && formula.resourceType == ResourceType.Health)
            {
                Debug.LogError("[RestorationCalculator] Health drain not supported. Use DamageEffect instead.");
                return 0;
            }

            int amount = DiceCalculator.Roll(formula.baseDice, formula.dieSize, formula.bonus);
            return formula.isDrain ? -amount : amount;
        }
    }
}
