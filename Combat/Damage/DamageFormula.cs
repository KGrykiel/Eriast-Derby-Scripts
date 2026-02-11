using UnityEngine;
using Assets.Scripts.Combat.Damage;

/// <summary>
/// Pure data specification for a single damage roll.
/// Just dice configuration - no weapon knowledge, no modes.
/// 
/// Weapon resolution happens upstream in DamageEffect.
/// Composite damage (weapon + bonus) uses multiple DamageEffects on the skill,
/// aggregated naturally by CombatEventBus.
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
