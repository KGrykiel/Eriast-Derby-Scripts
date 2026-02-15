using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts.Combat.SkillChecks;
using Assets.Scripts.Combat.Saves;
using Assets.Scripts.Combat.Damage;
using Assets.Scripts.StatusEffects;
using Assets.Scripts.Combat.Attacks;
using Assets.Scripts.Combat.Logging;

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
        
        public static void EmitRestoration(
            RestorationBreakdown breakdown,
            Entity source,
            Entity target,
            Object causalSource)
        {
            Emit(new RestorationEvent(breakdown, source, target, causalSource));
        }
        
        public static void EmitAttackRoll(
            AttackResult result,
            Entity source,
            Entity target,
            Object causalSource,
            bool isHit,
            string targetComponentName = null,
            bool isChassisFallback = false,
            Character character = null)
        {
            Emit(new AttackRollEvent(result, source, target, causalSource, isHit, targetComponentName, isChassisFallback, character));
        }
        
        public static void EmitSavingThrow(
            SaveResult result,
            Entity source,
            Entity target,
            Object causalSource,
            bool succeeded,
            string targetComponentName = null,
            Character character = null)
        {
            Emit(new SavingThrowEvent(result, source, target, causalSource, succeeded, targetComponentName, character));
        }
        
        public static void EmitSkillCheck(
            SkillCheckResult result,
            Entity source,
            Object causalSource,
            bool succeeded,
            Character character = null)
        {
            Emit(new SkillCheckEvent(result, source, causalSource, succeeded, character));
        }

        // ==================== UTILITY ====================

        //unused but might be helpful for testing or future features
        public static void Clear()
        {
            actionStack.Clear();
        }
    }
}
