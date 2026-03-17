using UnityEngine;

namespace Assets.Scripts.Combat.Restoration
{
    /// <summary>
    /// Serialisable pure data class for restoration amounts (health/energy).
    /// Same precedent as DamageFormula — no logic on the formula itself.
    /// Flat amount = baseDice 0, bonus N. Dice-based = baseDice > 0.
    /// </summary>
    [System.Serializable]
    public class RestorationFormula
    {
        [Tooltip("Which resource to restore/drain")]
        public ResourceType resourceType = ResourceType.Health;

        [Tooltip("Is this a drain (negative)? Only valid for Energy. Health drains must use DamageEffect.")]
        public bool isDrain = false;

        [Tooltip("Number of dice to roll (0 = flat amount only)")]
        public int baseDice = 0;

        [Tooltip("Size of each die (d4, d6, d8, d10, d12)")]
        public int dieSize = 6;

        [Tooltip("Flat bonus added to roll")]
        public int bonus = 0;
    }
}
