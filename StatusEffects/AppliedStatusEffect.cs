using System.Collections.Generic;
using Assets.Scripts.Combat.Damage;

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
                    int healing = periodicEffect.RollValue();
                    ApplyPeriodicHealing(healing);
                    break;
                
                case PeriodicEffectType.EnergyDrain:
                    int drainAmount = periodicEffect.RollValue();
                    ApplyPeriodicEnergyDrain(drainAmount);
                    break;
                
                case PeriodicEffectType.EnergyRestore:
                    int restoreAmount = periodicEffect.RollValue();
                    ApplyPeriodicEnergyRestore(restoreAmount);
                    break;
            }
        }
        
        private void ApplyPeriodicDamage(PeriodicEffectData periodicEffect)
        {
            var provider = new StaticFormulaProvider
            {
                formula = new DamageFormula
                {
                    baseDice = periodicEffect.diceCount,
                    dieSize = periodicEffect.dieSize,
                    bonus = periodicEffect.bonus,
                    damageType = periodicEffect.damageType
                }
            };

            var context = new FormulaContext(user: null);
            DamageFormula formula = provider.GetFormula(context);

            ResistanceLevel resistance = target.GetResistance(formula.damageType);
            DamageResult result = DamageCalculator.Compute(formula, resistance);
            DamageApplicator.Apply(result, target, attacker: null, template, DamageSource.Effect);
        }
        
        private void ApplyPeriodicHealing(int healing)
        {
            target.Heal(healing);
        }
        
        private void ApplyPeriodicEnergyDrain(int amount)
        {
            // TODO: Needs Vehicle/PowerCore integration
        }
        
        private void ApplyPeriodicEnergyRestore(int amount)
        {
            // TODO: Needs Vehicle/PowerCore integration
        }
        
        public bool PreventsActions => template.behavioralEffects?.preventsActions ?? false;
        public bool PreventsMovement => template.behavioralEffects?.preventsMovement ?? false;
        public float DamageAmplification => template.behavioralEffects?.damageAmplification ?? 1f;
    }
}
