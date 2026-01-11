using System;
using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts.StatusEffects;

namespace Assets.Scripts.Combat
{
    /// <summary>
    /// Central event bus for combat events.
    /// Manages action scoping for aggregated logging.
    /// 
    /// USAGE:
    /// 1. Skills/abilities call BeginAction() before applying effects
    /// 2. Effects emit events via Emit() 
    /// 3. Events are collected in the current action scope
    /// 4. EndAction() triggers CombatLogManager to aggregate and log
    /// 
    /// If no action scope is active, events are logged immediately (DoT, environmental, etc.)
    /// </summary>
    public static class CombatEventBus
    {
        // Stack supports nested actions (rare but possible)
        private static readonly Stack<CombatAction> actionStack = new Stack<CombatAction>();
        
        // ==================== ACTION SCOPING ====================
        
        /// <summary>
        /// Begin a new combat action scope.
        /// All events emitted until EndAction() are grouped together.
        /// </summary>
        /// <param name="actor">Entity performing the action</param>
        /// <param name="source">What triggered this (Skill, EventCard, etc.)</param>
        /// <param name="primaryTarget">Primary target vehicle (optional)</param>
        /// <returns>The created action (for reference, usually not needed)</returns>
        public static CombatAction BeginAction(Entity actor, UnityEngine.Object source, Vehicle primaryTarget = null)
        {
            var action = new CombatAction(actor, source, primaryTarget);
            actionStack.Push(action);
            return action;
        }
        
        /// <summary>
        /// End the current combat action scope.
        /// Triggers CombatLogManager to aggregate and log all events.
        /// </summary>
        public static void EndAction()
        {
            if (actionStack.Count == 0)
            {
                Debug.LogWarning("[CombatEventBus] EndAction called but no action is active!");
                return;
            }
            
            var action = actionStack.Pop();
            action.Complete();
            
            // Log the completed action
            CombatLogManager.LogAction(action);
        }
        
        /// <summary>
        /// Check if there's an active action scope.
        /// </summary>
        public static bool HasActiveAction => actionStack.Count > 0;
        
        /// <summary>
        /// Get the current action (if any).
        /// </summary>
        public static CombatAction CurrentAction => actionStack.Count > 0 ? actionStack.Peek() : null;
        
        // ==================== EVENT EMISSION ====================
        
        /// <summary>
        /// Emit a combat event.
        /// If an action scope is active, the event is added to it.
        /// Otherwise, the event is logged immediately.
        /// </summary>
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
        
        /// <summary>
        /// Emit a damage event.
        /// </summary>
        public static void EmitDamage(
            DamageBreakdown breakdown,
            Entity source,
            Entity target,
            UnityEngine.Object causalSource,
            DamageSource sourceType = DamageSource.Ability)
        {
            Emit(new DamageEvent(breakdown, source, target, causalSource, sourceType));
        }
        
        /// <summary>
        /// Emit a status effect applied event.
        /// </summary>
        public static void EmitStatusEffect(
            AppliedStatusEffect applied,
            Entity source,
            Entity target,
            UnityEngine.Object causalSource,
            bool wasReplacement = false)
        {
            Emit(new StatusEffectEvent(applied, source, target, causalSource, wasReplacement));
        }
        
        /// <summary>
        /// Emit a status effect expired event.
        /// </summary>
        public static void EmitStatusExpired(AppliedStatusEffect expired, Entity target)
        {
            Emit(new StatusEffectExpiredEvent(expired, target));
        }
        
        /// <summary>
        /// Emit a restoration event.
        /// </summary>
        public static void EmitRestoration(
            RestorationBreakdown breakdown,
            Entity source,
            Entity target,
            UnityEngine.Object causalSource)
        {
            Emit(new RestorationEvent(breakdown, source, target, causalSource));
        }
        
        /// <summary>
        /// Emit an attack roll event.
        /// </summary>
        public static void EmitAttackRoll(
            RollBreakdown roll,
            Entity source,
            Entity target,
            UnityEngine.Object causalSource,
            bool isHit,
            string targetComponentName = null,
            bool isChassisFallback = false)
        {
            Emit(new AttackRollEvent(roll, source, target, causalSource, isHit, targetComponentName, isChassisFallback));
        }
        
        // ==================== UTILITY ====================
        
        /// <summary>
        /// Clear all active actions (use for cleanup, scene transitions, etc.)
        /// </summary>
        public static void Clear()
        {
            actionStack.Clear();
        }
    }
}
