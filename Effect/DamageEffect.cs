using UnityEngine;
using RacingGame.Events;

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
    /// Logs damage events with appropriate importance based on amount and context.
    /// </summary>
    public override void Apply(Entity user, Entity target, Object context = null, Object source = null)
    {
        var vehicle = target as Vehicle;
        if (vehicle != null)
        {
            int damage = RollDamage();
            int oldHealth = vehicle.health;

            vehicle.TakeDamage(damage);

            // Old logging (keep for backwards compatibility)
            SimulationLogger.LogEvent($"{vehicle.vehicleName} takes {damage} damage.");

            // Note: Detailed damage logging is already handled by Vehicle.TakeDamage()
            // which includes damage amount, health percentages, and status changes.
            // We log here only the raw effect application for debugging purposes.

            string sourceText = source != null ? source.name : "unknown source";

            RaceHistory.Log(
                RacingGame.Events.EventType.Combat,
                EventImportance.Debug,
                $"[DMG] {sourceText} dealt {damage} damage to {vehicle.vehicleName} (rolled {damageDice}d{damageDieSize}+{damageBonus})",
                vehicle.currentStage,
                vehicle
            ).WithMetadata("damageRolled", damage)
             .WithMetadata("damageDice", damageDice)
             .WithMetadata("damageDieSize", damageDieSize)
             .WithMetadata("damageBonus", damageBonus)
             .WithMetadata("source", sourceText)
             .WithMetadata("oldHealth", oldHealth)
             .WithMetadata("newHealth", vehicle.health);
        }
        // right now focus on vehicle, will handle other entity types later
    }
}
