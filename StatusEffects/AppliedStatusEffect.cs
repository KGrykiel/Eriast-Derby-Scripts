using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts.Combat.Damage;

namespace Assets.Scripts.StatusEffects
{
    /// <summary>
    /// Runtime instance of an active status effect on an entity.
    /// Tracks duration, owns created modifiers, and handles periodic effects.
    /// 
    /// This is created when a StatusEffect template is applied to an entity.
    /// The StatusEffect is the template (ScriptableObject asset).
    /// AppliedStatusEffect is the active instance (runtime, per-entity).
    /// </summary>
    public class AppliedStatusEffect
    {
        /// <summary>
        /// Reference to the StatusEffect template (ScriptableObject asset).
        /// </summary>
        public StatusEffect template;
        
        /// <summary>
        /// The entity this status effect is applied to.
        /// </summary>
        public Entity target;
        
        /// <summary>
        /// Who/what applied this status effect (Skill, Stage, Component, etc.).
        /// Used for tracking source and for custom behaviors.
        /// </summary>
        public UnityEngine.Object applier;
        
        /// <summary>
        /// Remaining duration in turns. -1 = indefinite (permanent until dispelled).
        /// </summary>
        public int turnsRemaining;
        
        /// <summary>
        /// List of AttributeModifiers created by this status effect.
        /// These are automatically removed when the status effect expires or is removed.
        /// </summary>
        public List<AttributeModifier> createdModifiers = new List<AttributeModifier>();
        
        // ==================== PROPERTIES ====================
        
        /// <summary>
        /// Is this status effect indefinite (permanent until dispelled)?
        /// </summary>
        public bool IsIndefinite => turnsRemaining < 0;
        
        /// <summary>
        /// Has this status effect expired (duration reached 0)?
        /// </summary>
        public bool IsExpired => turnsRemaining == 0;
        
        // ==================== CONSTRUCTOR ====================
        
        /// <summary>
        /// Creates a new applied status effect instance.
        /// </summary>
        public AppliedStatusEffect(StatusEffect template, Entity target, UnityEngine.Object applier)
        {
            this.template = template;
            this.target = target;
            this.applier = applier;
            this.turnsRemaining = template.baseDuration;
        }
        
        // ==================== LIFECYCLE METHODS ====================
        
        /// <summary>
        /// Called when this status effect is first applied.
        /// Creates AttributeModifiers from template and applies them to target.
        /// </summary>
        public void OnApply()
        {
            // Create and apply stat modifiers
            foreach (var modData in template.modifiers)
            {
                var modifier = new AttributeModifier(
                    modData.attribute,
                    modData.type,
                    modData.value,
                    source: template  // StatusEffect is the source
                );
                
                createdModifiers.Add(modifier);
                target.AddModifier(modifier);
            }
            
            // TODO (Future): Custom behaviors
            // foreach (var behavior in template.customBehaviors)
            // {
            //     behavior.OnApply(target, applier);
            // }
        }
        
        /// <summary>
        /// Called at the start of each turn.
        /// Applies periodic effects (DoT, HoT, energy drain/restore).
        /// </summary>
        public void OnTick()
        {
            // Apply periodic effects
            foreach (var periodicEffect in template.periodicEffects)
            {
                ApplyPeriodicEffect(periodicEffect);
            }
            
            // TODO (Future): Custom behaviors
            // foreach (var behavior in template.customBehaviors)
            // {
            //     behavior.OnTick(target, applier);
            // }
        }
        
        /// <summary>
        /// Called when this status effect is removed (expired, dispelled, or entity destroyed).
        /// Cleans up all modifiers created by this effect.
        /// </summary>
        public void OnRemove()
        {
            // Remove all created modifiers
            foreach (var modifier in createdModifiers)
            {
                target.RemoveModifier(modifier);
            }
            
            createdModifiers.Clear();
            
            // TODO (Future): Custom behaviors
            // foreach (var behavior in template.customBehaviors)
            // {
            //     behavior.OnRemove(target, applier);
            // }
        }
        
        /// <summary>
        /// Decrements duration by 1 turn.
        /// Call this at the end of each turn.
        /// </summary>
        public void DecrementDuration()
        {
            if (IsIndefinite) return; // Indefinite effects don't expire
            
            turnsRemaining--;
        }
        
        // ==================== PERIODIC EFFECTS ====================
        
        /// <summary>
        /// Applies a single periodic effect to the target.
        /// </summary>
        private void ApplyPeriodicEffect(PeriodicEffectData periodicEffect)
        {
            switch (periodicEffect.type)
            {
                case PeriodicEffectType.Damage:
                    ApplyPeriodicDamage(periodicEffect.value, periodicEffect.damageType);
                    break;
                
                case PeriodicEffectType.Healing:
                    ApplyPeriodicHealing(periodicEffect.value);
                    break;
                
                case PeriodicEffectType.EnergyDrain:
                    ApplyPeriodicEnergyDrain(periodicEffect.value);
                    break;
                
                case PeriodicEffectType.EnergyRestore:
                    ApplyPeriodicEnergyRestore(periodicEffect.value);
                    break;
            }
        }
        
        private void ApplyPeriodicDamage(int damage, DamageType damageType)
        {
            // Apply environmental damage (no attacker entity)
            // Uses flat damage for now - future enhancement: support dice in PeriodicEffectData
            var breakdown = DamageApplicator.ApplyEnvironmentalFlat(
                damage: damage,
                damageType: damageType,
                target: target,
                causalSource: template,
                sourceType: DamageSource.Effect
            );
            
            // TODO: Log periodic damage
            // RaceHistory.Log($"{target.GetDisplayName()} takes {breakdown.finalDamage} {damageType} damage from {template.effectName}");
        }
        
        private void ApplyPeriodicHealing(int healing)
        {
            target.Heal(healing);
            
            // TODO: Log periodic healing
            // RaceHistory.Log(...);
        }
        
        private void ApplyPeriodicEnergyDrain(int amount)
        {
            // TODO (Phase 2): Implement energy drain on Entity/Vehicle
            // For now, skip (needs Vehicle/PowerCore integration)
            
            // Vehicle vehicle = target.GetComponent<Vehicle>();
            // if (vehicle != null)
            // {
            //     vehicle.energy = Mathf.Max(0, vehicle.energy - amount);
            // }
        }
        
        private void ApplyPeriodicEnergyRestore(int amount)
        {
            // TODO (Phase 2): Implement energy restore on Entity/Vehicle
            // For now, skip (needs Vehicle/PowerCore integration)
            
            // Vehicle vehicle = target.GetComponent<Vehicle>();
            // if (vehicle != null)
            // {
            //     vehicle.energy = Mathf.Min(vehicle.maxEnergy, vehicle.energy + amount);
            // }
        }
        
        // ==================== BEHAVIORAL QUERIES ====================
        
        /// <summary>
        /// Does this status effect prevent actions?
        /// Used by components to check if they can act.
        /// </summary>
        public bool PreventsActions => template.behavioralEffects?.preventsActions ?? false;
        
        /// <summary>
        /// Does this status effect prevent movement?
        /// Used by movement systems to check if entity can move.
        /// </summary>
        public bool PreventsMovement => template.behavioralEffects?.preventsMovement ?? false;
        
        /// <summary>
        /// Does this status effect prevent skill use?
        /// Used by skill systems to check if skills can be used.
        /// </summary>
        public bool PreventsSkillUse => template.behavioralEffects?.preventsSkillUse ?? false;
        
        /// <summary>
        /// Damage amplification multiplier from this effect.
        /// 1.0 = normal, 1.5 = take 50% more damage, 0.5 = take 50% less.
        /// </summary>
        public float DamageAmplification => template.behavioralEffects?.damageAmplification ?? 1f;
    }
}
