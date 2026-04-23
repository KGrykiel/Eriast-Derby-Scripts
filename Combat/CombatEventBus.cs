using System;
using System.Collections.Generic;
using UnityEngine;
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

        /// <summary>Fired immediately whenever a DamageEvent is emitted, before scoping/logging. Used for real-time visuals.</summary>
        public static event Action<DamageEvent> OnDamage;

        /// <summary>Fired immediately whenever a RestorationEvent is emitted, before scoping/logging. Used for real-time visuals.</summary>
        public static event Action<RestorationEvent> OnRestoration;

        /// <summary>Fired immediately whenever an AttackRollEvent is emitted, before scoping/logging. Used for real-time visuals.</summary>
        public static event Action<AttackRollEvent> OnAttackRoll;

        /// <summary>Fired when a scoped action is fully resolved, after logging. Used for statistics accumulation.</summary>
        public static event Action<CombatAction> OnActionCompleted;

        // NOTE: These three typed hooks do not scale. If visuals are needed for more event types,
        // introduce a dedicated CombatVisualBus with a single OnCombatEvent and remove these.
        // See FurtherRefinements.md A9.

        public static CombatAction BeginAction()
        {
            var action = new CombatAction();
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

            // Notify statistics tracker (and any other subscribers)
            OnActionCompleted?.Invoke(action);
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

            if (evt is DamageEvent dmg)
                OnDamage?.Invoke(dmg);

            if (evt is RestorationEvent restore)
                OnRestoration?.Invoke(restore);

            if (evt is AttackRollEvent attack)
                OnAttackRoll?.Invoke(attack);
            
            if (actionStack.Count > 0)
            {
                // Add to current action for aggregated logging
                actionStack.Peek().AddEvent(evt);
            }
            else
            {
                // No active scope (DoT, environmental, etc.) — auto-scope as a single-event action
                var tempAction = new CombatAction();
                tempAction.AddEvent(evt);
                CombatLogManager.LogAction(tempAction);
            }
        }
        
        // ==================== UTILITY ====================

        //unused but might be helpful for testing or future features
        public static void Clear()
        {
            actionStack.Clear();
        }
    }
}
