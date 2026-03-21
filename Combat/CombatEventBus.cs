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
            Entity actor,
            Object source,
            Vehicle primaryTarget = null,
            Vehicle sourceVehicle = null,
            Character sourceCharacter = null)
        {
            var action = new CombatAction(actor, source, primaryTarget, sourceVehicle, sourceCharacter);
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
            Object causalSource,
            DamageSource sourceType = DamageSource.Ability)
        {
            Emit(new DamageEvent(result, source, target, causalSource, sourceType));
        }
        
        public static void EmitStatusEffect(
            AppliedStatusEffect applied,
            Entity source,
            Entity target,
            Object causalSource,
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
            Entity target,
            Object causalSource)
        {
            Emit(new RestorationEvent(result, source, target, causalSource));
        }
        
        public static void EmitAttackRoll(
            D20RollOutcome roll,
            Entity source,
            Entity target,
            Object causalSource,
            bool isHit,
            string targetComponentName = null,
            Character character = null)
        {
            Emit(new AttackRollEvent(roll, source, target, causalSource, isHit, targetComponentName, character));
        }
        
        public static void EmitSavingThrow(
            D20RollOutcome roll,
            Entity source,
            Entity target,
            Object causalSource,
            bool succeeded,
            string checkName,
            bool isAutoFail = false,
            string targetComponentName = null,
            Character character = null)
        {
            Emit(new SavingThrowEvent(roll, source, target, causalSource, succeeded, checkName, isAutoFail, targetComponentName, character));
        }
        
        public static void EmitSkillCheck(
            D20RollOutcome roll,
            Entity source,
            Object causalSource,
            bool succeeded,
            string checkName,
            bool isAutoFail = false,
            Character character = null)
        {
            Emit(new SkillCheckEvent(roll, source, causalSource, succeeded, checkName, isAutoFail, character));
        }

        public static void EmitOpposedCheck(
            D20RollOutcome roll,
            D20RollOutcome defenderRoll,
            Entity attacker,
            Entity defender,
            Object causalSource,
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
