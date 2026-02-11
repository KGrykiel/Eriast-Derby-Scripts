using UnityEngine;

namespace Assets.Scripts.Combat.Damage
{
    /// <summary>
    /// Fixed damage formula that doesn't depend on context.
    /// Used for: Event cards, traps, environmental hazards, abilities with set damage.
    /// 
    /// Wraps a DamageFormula for Unity serialization and provider interface compliance.
    /// </summary>
    [System.Serializable]
    public class StaticFormulaProvider : IFormulaProvider
    {
        [Tooltip("Fixed damage formula (dice, bonus, damage type)")]
        public DamageFormula formula = new();

        public DamageFormula GetFormula(FormulaContext context)
        {
            return formula;
        }
    }
}
