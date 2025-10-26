using UnityEngine;

[System.Serializable]
public class DamageEffect : EffectBase
{
    public int damageDice = 0;      // Number of dice
    public int damageDieSize = 0;   // e.g. 6 for d6
    public int damageBonus = 0;

    /// <summary>
    /// Rolls the total damage (sum of dice + bonus).
    /// </summary>
    public int RollDamage()
    {
        return RollUtility.RollDamage(damageDice, damageDieSize, damageBonus);
    }

    /// <summary>
    /// Applies this damage effect to the target entity.
    /// </summary>
    public override void Apply(Entity user, Entity target, Object context = null, Object source = null)
    {
        var vehicle = target as Vehicle;
        if (vehicle != null)
        {
            int damage = RollDamage();
            vehicle.TakeDamage(damage);
            SimulationLogger.LogEvent($"{vehicle.vehicleName} takes {damage} damage.");
        }
        // right now focus on vehicle, will handle other entity types later
    }
}
