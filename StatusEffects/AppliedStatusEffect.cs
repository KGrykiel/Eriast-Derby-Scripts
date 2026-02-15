using System.Collections.Generic;
using Assets.Scripts.Combat.Damage;
using Assets.Scripts.Effects;

namespace Assets.Scripts.StatusEffects
{
    /// <summary>Runtime instance of a StatusEffect on an entity.</summary>
    public class AppliedStatusEffect
    {
        public StatusEffect template;
        public Entity target;
        public UnityEngine.Object applier;
        public int turnsRemaining;
        public List<AttributeModifier> createdModifiers = new();
        
        public bool IsIndefinite => turnsRemaining < 0;
        public bool IsExpired => turnsRemaining == 0;

        public AppliedStatusEffect(StatusEffect template, Entity target, UnityEngine.Object applier)
        {
            this.template = template;
            this.target = target;
            this.applier = applier;
            this.turnsRemaining = template.baseDuration;
        }
        
        public void OnApply()
        {
            foreach (var modData in template.modifiers)
            {
                var modifier = new AttributeModifier(
                    modData.attribute,
                    modData.type,
                    modData.value,
                    source: template,
                    category: ModifierCategory.StatusEffect
                );

                createdModifiers.Add(modifier);
                target.AddModifier(modifier);
            }
        }
        
        public void OnTick()
        {
            foreach (var periodicEffect in template.periodicEffects)
                ApplyPeriodicEffect(periodicEffect);
        }
        
        public void OnRemove()
        {
            foreach (var modifier in createdModifiers)
                target.RemoveModifier(modifier);

            createdModifiers.Clear();
        }
        
        public void DecrementDuration()
        {
            if (IsIndefinite) return;
            turnsRemaining--;
        }
        
        private void ApplyPeriodicEffect(PeriodicEffectData periodicEffect)
        {
            switch (periodicEffect.type)
            {
                case PeriodicEffectType.Damage:
                    ApplyPeriodicDamage(periodicEffect);
                    break;

                case PeriodicEffectType.Healing:
                    ApplyPeriodicRestoration(periodicEffect, ResourceRestorationEffect.ResourceType.Health, periodicEffect.amount);
                    break;

                case PeriodicEffectType.EnergyDrain:
                    ApplyPeriodicRestoration(periodicEffect, ResourceRestorationEffect.ResourceType.Energy, -periodicEffect.amount);
                    break;

                case PeriodicEffectType.EnergyRestore:
                    ApplyPeriodicRestoration(periodicEffect, ResourceRestorationEffect.ResourceType.Energy, periodicEffect.amount);
                    break;
            }
        }

        private void ApplyPeriodicDamage(PeriodicEffectData periodicEffect)
        {
            var effect = new DamageEffect
            {
                formulaProvider = new StaticFormulaProvider { formula = periodicEffect.damageFormula }
            };

            var context = new EffectContext { DamageSourceOverride = DamageSource.Effect };
            effect.Apply(target, context, template);
        }

        private void ApplyPeriodicRestoration(PeriodicEffectData periodicEffect, ResourceRestorationEffect.ResourceType resourceType, int amount)
        {
            var effect = new ResourceRestorationEffect
            {
                resourceType = resourceType,
                amount = amount
            };

            effect.Apply(target, EffectContext.Default, template);
        }
        
        public bool PreventsActions => template.behavioralEffects?.preventsActions ?? false;
        public bool PreventsMovement => template.behavioralEffects?.preventsMovement ?? false;
        public float DamageAmplification => template.behavioralEffects?.damageAmplification ?? 1f;
    }
}
