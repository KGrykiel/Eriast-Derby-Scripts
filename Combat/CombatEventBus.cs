using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts.Combat.Damage;
using Assets.Scripts.Combat.Restoration;
using Assets.Scripts.StatusEffects;
using Assets.Scripts.Combat.Logging;
using Assets.Scripts.Combat.Rolls;

namespace Assets.Scripts.Combat
{
    /// <summary>
    /// Central event bus with action scoping for aggregated logging - particularly for samage with multiple damage types.
    /// Events emitted during a BeginAction/EndAction scope are grouped and logged together.
    /// Events outside a scope are logged immediately (DoT, environmental, etc).
    /// </summary>
    public static class CombatEventBus
    {
        // Stack supports nested actions (just in case)
        private static readonly Stack<CombatAction> actionStack = new();

        public static CombatAction BeginAction(
            RollActor sourceActor,
            string source,
            Vehicle primaryTarget = null,
            Vehicle sourceVehicle = null)
        {
            var action = new CombatAction(sourceActor, source, primaryTarget, sourceVehicle);
            actionStack.Push(action);
            return action;
        }
        
        public static void EndAction()
        {
            if (actionStack.Count == 0)
            {
                Debug.LogWarning("[CombatEventBus] EndAction called but no action is active!");
                return;
            }
            
            var action = actionStack.Pop();

            // Log the completed action
            CombatLogManager.LogAction(action);
        }
        
        // ==================== EVENT EMISSION ====================

        /// <summary>Scoped = collect in action, unscoped = log immediately.</summary>
        public static void Emit(CombatEvent evt)
        {
            if (evt == null)
            {
                Debug.LogWarning("[CombatEventBus] Attempted to emit null event!");
                return;
            }
            
            if (actionStack.Count > 0)
            {
                // Add to current action for aggregated logging
                actionStack.Peek().AddEvent(evt);
            }
            else
            {
                // No action scope - log immediately (DoT, environmental, etc.)
                CombatLogManager.LogImmediate(evt);
            }
        }
        
        // ==================== CONVENIENCE METHODS ====================

        public static void EmitDamage(
            DamageResult result,
            Entity source,
            Entity target,
            string causalSource,
            DamageSource sourceType = DamageSource.Ability)
        {
            Emit(new DamageEvent(result, source, target, causalSource, sourceType));
        }
        
        public static void EmitStatusEffect(
            AppliedStatusEffect applied,
            Entity source,
            Entity target,
            string causalSource,
            bool wasReplacement = false)
        {
            Emit(new StatusEffectEvent(applied, source, target, causalSource, wasReplacement));
        }

        public static void EmitStatusExpired(AppliedStatusEffect expired, Entity target)
        {
            Emit(new StatusEffectExpiredEvent(expired, target));
        }

        public static void EmitStatusRefreshed(AppliedStatusEffect refreshed, Entity target)
        {
            Emit(new StatusEffectRefreshedEvent(refreshed, target));
        }

        public static void EmitStatusIgnored(AppliedStatusEffect existing, Entity target)
        {
            Emit(new StatusEffectIgnoredEvent(existing, target));
        }

        public static void EmitStatusReplaced(AppliedStatusEffect newEffect, Entity target, int oldDuration)
        {
            Emit(new StatusEffectReplacedEvent(newEffect, target, oldDuration));
        }

        public static void EmitStatusKeptStronger(AppliedStatusEffect kept, Entity target)
        {
            Emit(new StatusEffectKeptStrongerEvent(kept, target));
        }

        public static void EmitStatusStackLimit(StatusEffect template, Entity target, int maxStacks)
        {
            Emit(new StatusEffectStackLimitEvent(template, target, maxStacks));
        }

        public static void EmitRestoration(
            RestorationResult result,
            Entity source,
            Entity target)
        {
            Emit(new RestorationEvent(result, source, target));
        }
        
        public static void EmitAttackRoll(
            D20RollOutcome roll,
            RollActor actor,
            Entity target,
            string causalSource)
        {
            Emit(new AttackRollEvent(roll, actor, target, causalSource));
        }
        
        public static void EmitSavingThrow(
            D20RollOutcome roll,
            Entity source,
            RollActor defender,
            string causalSource,
            string checkName)
        {
            Emit(new SavingThrowEvent(roll, source, defender, causalSource, checkName));
        }
        
        public static void EmitSkillCheck(
            D20RollOutcome roll,
            RollActor actor,
            string causalSource,
            string checkName)
        {
            Emit(new SkillCheckEvent(roll, actor, causalSource, checkName));
        }

        public static void EmitOpposedCheck(
            D20RollOutcome roll,
            D20RollOutcome defenderRoll,
            RollActor attacker,
            RollActor defender,
            string causalSource,
            string attackerCheckName,
            string defenderCheckName)
        {
            Emit(new OpposedCheckEvent(roll, defenderRoll, attacker, defender, causalSource, attackerCheckName, defenderCheckName));
        }

        // ==================== UTILITY ====================

        //unused but might be helpful for testing or future features
        public static void Clear()
        {
            actionStack.Clear();
        }
    }
}
