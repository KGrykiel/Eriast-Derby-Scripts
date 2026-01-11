using System.Collections.Generic;
using System.Linq;
using RacingGame.Events;
using EventType = RacingGame.Events.EventType;
using Assets.Scripts.Combat;

namespace Assets.Scripts.Skills.Helpers
{
    /// <summary>
    /// DEPRECATED: Combat logging is now handled by CombatLogManager via CombatEventBus.
    /// 
    /// This class is kept for backwards compatibility but most methods have been removed.
    /// Use CombatEventBus.Emit*() methods instead.
    /// </summary>
    public static class SkillCombatLogger
    {
        // All logging is now handled by CombatLogManager via CombatEventBus.
        // Effects emit events → CombatEventBus collects them → CombatLogManager formats and logs.
        //
        // See:
        // - CombatEventBus.EmitDamage()
        // - CombatEventBus.EmitStatusEffect()
        // - CombatEventBus.EmitRestoration()
        // - CombatEventBus.EmitAttackRoll()
    }
}
