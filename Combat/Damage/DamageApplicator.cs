using Assets.Scripts.Combat.Rolls;

namespace Assets.Scripts.Combat.Damage
{
    /// <summary>
    /// Single entry point for ALL damage in the game — skills, DoT, hazards, event cards, maybe more in the future.
    /// Guarantees every damage source gets event emission and consistent handling.
    /// </summary>
    public static class DamageApplicator
    {
        /// <summary>
        /// Resistance must already be resolved by DamageCalculator.
        /// Emits even on 0 damage — needed for IMMUNE/RESISTANT feedback.
        /// </summary>
        public static DamageResult Apply(
            DamageResult result,
            Entity target,
            RollActor actor = null,
            string causalSource = null)
        {
            // ALWAYS emit event for logging, even if damage is 0
            // This is critical for showing IMMUNE/RESISTANT feedback to players
            CombatEventBus.Emit(new DamageEvent(result, actor, target, causalSource));

            // Apply damage to target
            if (result.FinalDamage > 0)
            {
                target.TakeDamage(result.FinalDamage);
            }

            return result;
        }
    }
}
