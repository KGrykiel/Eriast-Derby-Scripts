using UnityEngine;
using RacingGame.Events;

[System.Serializable]
public class DamageEffect : EffectBase
{
    public int damageDice = 0;      // Number of dice
    public int damageDieSize = 0;   // e.g. 6 for d6
    public int damageBonus = 0;

    // Store last rolled damage for retrieval by Skill.Use()
    private int lastDamageRolled = 0;

    /// <summary>
    /// Gets the last damage rolled by this effect.
    /// Used by Skill.Use() to log accurate damage values.
    /// </summary>
    public int LastDamageRolled => lastDamageRolled;

    /// <summary>
    /// Rolls the total damage (sum of dice + bonus).
    /// </summary>
    public int RollDamage()
    {
        return RollUtility.RollDamage(damageDice, damageDieSize, damageBonus);
    }

    /// <summary>
    /// Applies this damage effect to the target entity.
    /// NO LOGGING - handled by Skill.Use() to prevent duplicates.
    /// </summary>
    public override void Apply(Entity user, Entity target, Object context = null, Object source = null)
    {
        lastDamageRolled = RollDamage();
        target.TakeDamage(lastDamageRolled);

        // Logging removed - Skill.Use() handles comprehensive combat logging
        // This prevents duplicate damage events
    }
}
