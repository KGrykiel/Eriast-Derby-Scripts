using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets.Scripts.Conditions
{
    /// <summary>
    /// Abstract base for per-owner condition/effect managers.
    /// Handles stacking, duration, removal triggers, and expiry.
    /// Subclasses provide validation, construction, activation lifecycle, and event hooks.
    /// </summary>
    public abstract class ConditionManagerBase<TTemplate, TApplied>
        where TTemplate : ConditionBase
        where TApplied : AppliedConditionBase
    {
        protected readonly List<TApplied> activeConditions = new();

        // ==================== PUBLIC API ====================

        public TApplied Apply(TTemplate template, Object applier)
        {
            if (!CanApply(template))
            {
                Debug.LogWarning($"[Conditions] Cannot apply {template.effectName} to {GetOwnerDisplayName()}");
                return null;
            }

            return template.stackBehaviour switch
            {
                StackBehaviour.Refresh => ApplyWithRefresh(template, applier),
                StackBehaviour.Stack => ApplyWithStack(template, applier),
                StackBehaviour.Ignore => ApplyWithIgnore(template, applier),
                StackBehaviour.Replace => ApplyWithReplace(template, applier),
                _ => ApplyWithRefresh(template, applier)
            };
        }

        public void Remove(TApplied applied)
        {
            if (activeConditions.Remove(applied))
                OnDeactivate(applied);
        }

        public List<TApplied> GetActive() => activeConditions;

        public void OnTurnStart()
        {
            ProcessRemovalTrigger(RemovalTrigger.OnTurnStart);

            foreach (var applied in activeConditions.ToList())
                OnTick(applied);

            for (int i = activeConditions.Count - 1; i >= 0; i--)
            {
                var applied = activeConditions[i];
                applied.DecrementDuration();

                if (applied.IsExpired)
                {
                    OnExpired(applied);
                    OnDeactivate(applied);
                    activeConditions.RemoveAt(i);
                }
            }
        }

        public void Cleanup()
        {
            foreach (var applied in activeConditions)
                OnDeactivate(applied);
            activeConditions.Clear();
        }

        public void RemoveByCategory(ConditionCategory categories)
        {
            var toRemove = activeConditions
                .Where(c => (c.Template.categories & categories) != 0)
                .ToList();

            foreach (var applied in toRemove)
                Remove(applied);
        }

        public void RemoveByTemplate(TTemplate template)
        {
            var toRemove = activeConditions
                .Where(c => c.Template == template)
                .ToList();

            foreach (var applied in toRemove)
                Remove(applied);
        }

        public void ProcessRemovalTrigger(RemovalTrigger trigger)
        {
            var toRemove = activeConditions
                .Where(c => c.Template.removalTriggers.HasFlag(trigger))
                .ToList();

            foreach (var applied in toRemove)
            {
                OnRemovedByTrigger(applied, trigger);
                Remove(applied);
            }
        }

        // ==================== ABSTRACT ====================

        protected abstract bool CanApply(TTemplate template);
        protected abstract TApplied CreateApplied(TTemplate template, Object applier);
        protected abstract float GetTemplateMagnitude(TTemplate template);
        protected abstract string GetOwnerDisplayName();

        // ==================== VIRTUAL HOOKS ====================

        protected virtual void OnActivate(TApplied applied) { }
        protected virtual void OnDeactivate(TApplied applied) { }
        protected virtual void OnTick(TApplied applied) { }
        protected virtual void OnExpired(TApplied applied) { }
        protected virtual void OnNewlyApplied(TApplied applied, bool wasReplacement) { }
        protected virtual void OnRefreshed(TApplied applied) { }
        protected virtual void OnIgnored(TApplied applied) { }
        protected virtual void OnStackLimitReached(TTemplate template) { }
        protected virtual void OnReplaced(TApplied newApplied, int oldDuration) { }
        protected virtual void OnKeptStronger(TApplied applied) { }
        protected virtual void OnRemovedByTrigger(TApplied applied, RemovalTrigger trigger) { }

        // ==================== STACKING LOGIC ====================

        private TApplied ApplyWithRefresh(TTemplate template, Object applier)
        {
            var existing = activeConditions.FirstOrDefault(c => c.Template == template);

            if (existing != null)
            {
                existing.RefreshDuration();
                OnRefreshed(existing);
                return existing;
            }

            return CreateAndActivate(template, applier, wasReplacement: false);
        }

        private TApplied ApplyWithStack(TTemplate template, Object applier)
        {
            int currentStackCount = activeConditions.Count(c => c.Template == template);

            if (template.maxStacks > 0 && currentStackCount >= template.maxStacks)
            {
                OnStackLimitReached(template);
                return null;
            }

            return CreateAndActivate(template, applier, wasReplacement: false);
        }

        private TApplied ApplyWithIgnore(TTemplate template, Object applier)
        {
            var existing = activeConditions.FirstOrDefault(c => c.Template == template);

            if (existing != null)
            {
                OnIgnored(existing);
                return existing;
            }

            return CreateAndActivate(template, applier, wasReplacement: false);
        }

        private TApplied ApplyWithReplace(TTemplate template, Object applier)
        {
            var existing = activeConditions.FirstOrDefault(c => c.Template == template);

            if (existing != null)
            {
                if (IsStronger(template, existing))
                {
                    int oldDuration = existing.turnsRemaining;
                    OnDeactivate(existing);
                    activeConditions.Remove(existing);
                    var newApplied = CreateAndActivate(template, applier, wasReplacement: true);
                    OnReplaced(newApplied, oldDuration);
                    return newApplied;
                }
                else
                {
                    OnKeptStronger(existing);
                    return existing;
                }
            }

            return CreateAndActivate(template, applier, wasReplacement: false);
        }

        private TApplied CreateAndActivate(TTemplate template, Object applier, bool wasReplacement)
        {
            var applied = CreateApplied(template, applier);
            OnActivate(applied);
            activeConditions.Add(applied);
            OnNewlyApplied(applied, wasReplacement);
            return applied;
        }

        private bool IsStronger(TTemplate newTemplate, TApplied existing)
        {
            float existingMagnitude = GetTemplateMagnitude((TTemplate)existing.Template);
            float newMagnitude = GetTemplateMagnitude(newTemplate);

            if (newMagnitude > existingMagnitude)
                return true;
            if (newMagnitude < existingMagnitude)
                return false;

            bool newIsIndefinite = newTemplate.baseDuration == -1;
            bool existingIsIndefinite = existing.turnsRemaining == -1;

            if (newIsIndefinite && !existingIsIndefinite)
                return true;
            if (!newIsIndefinite && existingIsIndefinite)
                return false;

            return newTemplate.baseDuration > existing.turnsRemaining;
        }
    }
}
