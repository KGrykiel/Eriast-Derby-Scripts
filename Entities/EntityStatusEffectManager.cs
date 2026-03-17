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

            var existing = activeEffects.FirstOrDefault(a => a.template == effect);
            bool wasReplacement = false;

            if (existing != null)
            {
                if (ShouldReplace(existing, effect))
                {
                    existing.Deactivate();
                    activeEffects.Remove(existing);
                    wasReplacement = true;
                }
                else
                {
                    return existing;
                }
            }

            var applied = new AppliedStatusEffect(effect, owner, applier);
            applied.Activate();
            activeEffects.Add(applied);

            Entity sourceEntity = applier as Entity;
            CombatEventBus.EmitStatusEffect(applied, sourceEntity, owner, applier, wasReplacement);

            return applied;
        }

        public void Remove(AppliedStatusEffect effect)
        {
            if (activeEffects.Remove(effect))
            {
                effect.Deactivate();
            }
        }

        /// <summary>Removes all effects from a specific source (e.g. leaving a lane).</summary>
        public void RemoveFromSource(Object source)
        {
            if (source == null) return;

            var toRemove = activeEffects.Where(e => e.applier == source).ToList();
            foreach (var effect in toRemove)
            {
                Remove(effect);
            }
        }

        public List<AppliedStatusEffect> GetActive() => activeEffects;

        /// <summary>Ticks periodic effects, decrements durations, removes expired. Called by handler each turn.</summary>
        public void OnTurnStart()
        {
            foreach (var effect in activeEffects.ToList())
            {
                effect.OnTick();
            }

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
            foreach (var effect in activeEffects)
            {
                effect.Deactivate();
            }
            activeEffects.Clear();
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

        /// <summary>Stacking rules: higher magnitude wins, then longer duration.</summary>
        private bool ShouldReplace(AppliedStatusEffect existing, StatusEffect newEffect)
        {
            float existingMagnitude = existing.template.modifiers.Sum(m => Mathf.Abs(m.value));
            float newMagnitude = newEffect.modifiers.Sum(m => Mathf.Abs(m.value));

            if (newMagnitude > existingMagnitude)
                return true;
            if (newMagnitude < existingMagnitude)
                return false;

            int existingDuration = existing.turnsRemaining;
            int newDuration = newEffect.baseDuration;

            if (newDuration > existingDuration)
                return true;

            return false;
        }
    }
}
