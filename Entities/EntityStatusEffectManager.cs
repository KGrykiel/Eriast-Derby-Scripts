using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.StatusEffects;
using Assets.Scripts.Combat;
using UnityEngine;

namespace Assets.Scripts.Entities
{
    /// <summary>
    /// Per-entity manager for status effects. Owns the collection, handles stacking,
    /// orchestrates lifecycle (apply/tick/expire/remove). WOTR BuffCollection pattern.
    /// Called directly by phase handlers via Vehicle → Entity delegation.
    /// </summary>
    public class EntityStatusEffectManager
    {
        private readonly Entity owner;
        private readonly List<AppliedStatusEffect> activeEffects = new();

        public EntityStatusEffectManager(Entity owner)
        {
            this.owner = owner;
            owner.OnDamaged += HandleOwnerDamaged;
        }

        // ==================== PUBLIC API ====================

        /// <summary>Validates, handles stacking, creates and activates the effect. Returns null if validation fails.</summary>
        public AppliedStatusEffect Apply(StatusEffect effect, Object applier)
        {
            if (!CanApply(effect))
            {
                Debug.LogWarning($"[StatusEffects] Cannot apply {effect.effectName} to {owner.GetDisplayName()} - feature requirements not met");
                return null;
            }

            return effect.stackBehaviour switch
            {
                StackBehaviour.Refresh => ApplyWithRefresh(effect, applier),
                StackBehaviour.Stack => ApplyWithStack(effect, applier),
                StackBehaviour.Ignore => ApplyWithIgnore(effect, applier),
                StackBehaviour.Replace => ApplyWithReplace(effect, applier),
                _ => ApplyWithRefresh(effect, applier)
            };
        }

        public void Remove(AppliedStatusEffect effect)
        {
            if (activeEffects.Remove(effect))
            {
                effect.Deactivate();
            }
        }

        public List<AppliedStatusEffect> GetActive() => activeEffects;

        /// <summary>Ticks periodic effects, decrements durations, removes expired. Called by handler each turn.</summary>
        public void OnTurnStart()
        {
            // 1. Remove effects that expire at turn start BEFORE they tick (D&D 5e timing)
            ProcessRemovalTrigger(RemovalTrigger.OnTurnStart);

            // 2. Tick periodic effects (DoT, HoT)
            foreach (var effect in activeEffects.ToList())
            {
                effect.OnTick();
            }

            // 3. Decrement durations and remove expired
            for (int i = activeEffects.Count - 1; i >= 0; i--)
            {
                var effect = activeEffects[i];

                effect.DecrementDuration();

                if (effect.IsExpired)
                {
                    CombatEventBus.EmitStatusExpired(effect, owner);
                    effect.Deactivate();
                    activeEffects.RemoveAt(i);
                }
            }
        }

        /// <summary>Deactivates all effects and clears the collection during owner teardown.</summary>
        public void Cleanup()
        {
            owner.OnDamaged -= HandleOwnerDamaged;

            foreach (var effect in activeEffects)
            {
                effect.Deactivate();
            }
            activeEffects.Clear();
        }

        /// <summary>Removes all effects matching the specified categories (skill-based dispel).</summary>
        public void RemoveByCategory(EffectCategory categories)
        {
            var toRemove = activeEffects
                .Where(e => (e.template.categories & categories) != 0)
                .ToList();

            foreach (var effect in toRemove)
            {
                Remove(effect);
            }
        }

        /// <summary>Removes all instances of a specific template (targeted dispel).</summary>
        public void RemoveByTemplate(StatusEffect template)
        {
            var toRemove = activeEffects
                .Where(e => e.template == template)
                .ToList();

            foreach (var effect in toRemove)
            {
                Remove(effect);
            }
        }

        /// <summary>Removes all effects matching the specified trigger flag. Called by Entity.NotifyStatusEffectTrigger and internally.</summary>
        public void ProcessRemovalTrigger(RemovalTrigger trigger)
        {
            var toRemove = activeEffects
                .Where(e => e.template.removalTriggers.HasFlag(trigger))
                .ToList();

            foreach (var effect in toRemove)
            {
                Remove(effect);
            }
        }

        // ==================== EVENT HANDLERS ====================

        private void HandleOwnerDamaged(int amount)
        {
            ProcessRemovalTrigger(RemovalTrigger.OnDamageTaken);
        }

        // ==================== PRIVATE HELPERS ====================

        private bool CanApply(StatusEffect effect)
        {
            if (effect.requiredFeatures != EntityFeature.None)
            {
                if (!owner.HasFeature(effect.requiredFeatures))
                    return false;
            }

            if (effect.excludedFeatures != EntityFeature.None)
            {
                if (owner.HasAnyFeature(effect.excludedFeatures))
                    return false;
            }

            return true;
        }

        // ==================== STACKING LOGIC ====================

        private AppliedStatusEffect ApplyWithRefresh(StatusEffect effect, Object applier)
        {
            var existing = activeEffects.FirstOrDefault(a => a.template == effect);

            if (existing != null)
            {
                existing.RefreshDuration();
                CombatEventBus.EmitStatusRefreshed(existing, owner);
                return existing;
            }

            return CreateAndActivate(effect, applier, wasReplacement: false);
        }

        private AppliedStatusEffect ApplyWithStack(StatusEffect effect, Object applier)
        {
            int currentStackCount = activeEffects.Count(a => a.template == effect);

            if (effect.maxStacks > 0 && currentStackCount >= effect.maxStacks)
            {
                CombatEventBus.EmitStatusStackLimit(effect, owner, effect.maxStacks);
                return null;
            }

            return CreateAndActivate(effect, applier, wasReplacement: false);
        }

        private AppliedStatusEffect ApplyWithIgnore(StatusEffect effect, Object applier)
        {
            var existing = activeEffects.FirstOrDefault(a => a.template == effect);

            if (existing != null)
            {
                CombatEventBus.EmitStatusIgnored(existing, owner);
                return existing;
            }

            return CreateAndActivate(effect, applier, wasReplacement: false);
        }

        private AppliedStatusEffect ApplyWithReplace(StatusEffect effect, Object applier)
        {
            var existing = activeEffects.FirstOrDefault(a => a.template == effect);

            if (existing != null)
            {
                if (IsStronger(effect, existing))
                {
                    int oldDuration = existing.turnsRemaining;
                    existing.Deactivate();
                    activeEffects.Remove(existing);
                    var newEffect = CreateAndActivate(effect, applier, wasReplacement: true);
                    CombatEventBus.EmitStatusReplaced(newEffect, owner, oldDuration);
                    return newEffect;
                }
                else
                {
                    CombatEventBus.EmitStatusKeptStronger(existing, owner);
                    return existing;
                }
            }

            return CreateAndActivate(effect, applier, wasReplacement: false);
        }

        /// <summary>Helper: creates, activates, adds to list, emits event.</summary>
        private AppliedStatusEffect CreateAndActivate(StatusEffect effect, Object applier, bool wasReplacement)
        {
            var applied = new AppliedStatusEffect(effect, owner, applier);
            applied.Activate();
            activeEffects.Add(applied);

            Entity sourceEntity = applier as Entity;
            CombatEventBus.EmitStatusEffect(applied, sourceEntity, owner, applier?.name, wasReplacement);

            return applied;
        }

        /// <summary>Compares strength: higher magnitude wins, then longer duration.</summary>
        private bool IsStronger(StatusEffect newEffect, AppliedStatusEffect existing)
        {
            float existingMagnitude = existing.template.modifiers.Sum(m => Mathf.Abs(m.value));
            float newMagnitude = newEffect.modifiers.Sum(m => Mathf.Abs(m.value));

            if (newMagnitude > existingMagnitude)
                return true;
            if (newMagnitude < existingMagnitude)
                return false;

            bool newIsIndefinite = newEffect.baseDuration == -1;
            bool existingIsIndefinite = existing.turnsRemaining == -1;

            if (newIsIndefinite && !existingIsIndefinite)
                return true;
            if (!newIsIndefinite && existingIsIndefinite)
                return false;

            return newEffect.baseDuration > existing.turnsRemaining;
        }
    }
}
