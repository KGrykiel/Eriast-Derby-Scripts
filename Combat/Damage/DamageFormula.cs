using UnityEngine;

namespace Assets.Scripts.Combat.Damage
{
    /// <summary>
    /// Formula expressing damage for a single unit of damage. Composite damage can be achieved by applying multiple DamageEffects in one skill, each with its own formula.
    /// </summary>
    [System.Serializable]
    public class DamageFormula
    {
        [Tooltip("Number of dice to roll")]
        public int baseDice = 1;

        [Tooltip("Size of each die (d4, d6, d8, d10, d12)")]
        public int dieSize = 6;

        [Tooltip("Flat bonus added to damage")]
        public int bonus = 0;

        [Tooltip("Type of damage dealt")]
        public DamageType damageType = DamageType.Physical;
    }
}
