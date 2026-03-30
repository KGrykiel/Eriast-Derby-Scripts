using UnityEngine;
using System;
using System.Collections.Generic;
using Assets.Scripts.Combat.Damage;
using Assets.Scripts.Combat.Rolls.Advantage;
using Assets.Scripts.Core;
using Assets.Scripts.Conditions.EntityConditions;
using Assets.Scripts.Conditions;

using Assets.Scripts.Effects;

namespace Assets.Scripts.Entities
{
    /// <summary>
    /// Base class for anything with HP that can be damaged/targeted.
    /// Note: Vehicle itself is NOT an Entity, it's a container for Entity components.
    /// </summary>
    public abstract class Entity : MonoBehaviour, IRollTarget, IEffectTarget
    {
        [Header("Entity Stats")]
        [Tooltip("Current health points")]
        [SerializeField]
        protected int health = 100;
        
        [SerializeField]
        [Tooltip("Maximum health points (base value before modifiers)")]
        protected int baseMaxHealth = 100;
        
        [SerializeField]
        [Tooltip("Armor class (difficulty to hit) (base value before modifiers)")]
        protected int baseArmorClass = 10;
        
        [Header("Entity State")]
        [Tooltip("Is this entity destroyed?")]
        [SerializeField] private bool isDestroyed = false;
        public bool IsDestroyed() => isDestroyed;
        
        [Header("Entity Features")]
        [Tooltip("Capability flags for status effect targeting validation")]
        public EntityFeature features = EntityFeature.None;
        
        [Header("Damage Resistances")]
        [Tooltip("Resistances and vulnerabilities to different damage types")]
        public List<DamageResistance> resistances = new();
    
        // ==================== MODIFIER & STATUS EFFECT STORAGE ====================
    
        [SerializeField, HideInInspector]
        protected List<AttributeModifier> entityModifiers = new();
    
        private readonly EntityConditionManager statusEffects;
    
        protected Entity()
        {
            statusEffects = new EntityConditionManager(this);
        }
    
        // ==================== STAT ACCESSORS ====================
        // Naming convention:
        // - GetBaseStat() returns raw field value (no modifiers)
        // - GetStat() returns effective value (with modifiers via StatCalculator)
        // Game code should almost always use GetStat() for gameplay calculations.
        
        // Runtime state accessor
        public int GetCurrentHealth() => health;
        
        // Base value accessors (return raw field values without modifiers)
        public int GetBaseMaxHealth() => baseMaxHealth;
        public int GetBaseArmorClass() => baseArmorClass;
        
        // Modified value accessors (return values with all modifiers applied via StatCalculator)
        public virtual int GetMaxHealth() => StatCalculator.GatherAttributeValue(this, Attribute.MaxHealth);
        public virtual int GetArmorClass() => StatCalculator.GatherAttributeValue(this, Attribute.ArmorClass);
        
        // ==================== ENTITY FEATURES ====================
        
        public bool HasFeature(EntityFeature feature)
        {
            return (features & feature) == feature;
        }
    
        public bool HasAnyFeature(EntityFeature flags)
        {
            return (features & flags) != 0;
        }
    
        // ==================== DAMAGE RESISTANCES ====================
    
        /// <summary>Returns Normal if no resistance defined.</summary>
        public virtual ResistanceLevel GetResistance(DamageType type)
        {
            // Find resistance entry for this damage type
            foreach (var resistance in resistances)
            {
                if (resistance.type == type)
                    return resistance.level;
            }
    
            return ResistanceLevel.Normal;
        }
    
        // ==================== DAMAGE & HEALING ====================
    
        public virtual void TakeDamage(int amount)
        {
            if (isDestroyed) return;
    
            int previousHealth = health;
            health = Mathf.Max(health - amount, 0);
    
            OnDamageTaken(amount, previousHealth, health);
    
            if (amount > 0)
            {
                OnDamaged?.Invoke(amount);
                NotifyConditionTrigger(RemovalTrigger.OnDamageTaken);
            }
    
            if (health <= 0 && !isDestroyed)
            {
                isDestroyed = true;
                OnEntityDestroyed();
            }
        }
    
        protected virtual void OnDamageTaken(int amount, int previousHealth, int newHealth){}
    
        protected virtual void OnEntityDestroyed()
        {
            statusEffects.Cleanup();
        }
    
        public virtual void Heal(int amount)
        {
            if (isDestroyed) return;
    
            health = Mathf.Min(health + amount, GetMaxHealth());
        }
    
        /// <summary>Sets health directly. Bypasses isDestroyed check — use for initialization and test setup.</summary>
        public void SetHealth(int value)
        {
            health = Mathf.Clamp(value, 0, GetMaxHealth());
        }
    
        /// <summary>Resets destroyed state directly. Use for restoration mechanics and test setup only.</summary>
        public void ResetDestroyedState()
        {
            isDestroyed = false;
        }
    
        // ==================== MODIFIER SYSTEM ====================
        
        /// <summary>Components use this for permanent equipment bonuses. Skills should use ApplyStatusEffect().</summary>
        public virtual void AddModifier(AttributeModifier modifier)
        {
            entityModifiers.Add(modifier);
        }
    
        public virtual void RemoveModifier(AttributeModifier modifier)
        {
            entityModifiers.Remove(modifier);
        }
    
        public void RemoveModifiersFromSource(object source)
        {
            entityModifiers.RemoveAll(m => m.Source == source);
        }
    
        public virtual IReadOnlyList<AttributeModifier> GetModifiers()
        {
            return entityModifiers;
        }
    
        // ==================== ADVANTAGE GRANT SYSTEM ====================
    
        [NonSerialized]
        private List<AdvantageGrant> _advantageGrants = new();
    
        public void AddAdvantageGrant(AdvantageGrant grant) => _advantageGrants.Add(grant);
        public void RemoveAdvantageGrantsFromSource(object source) => _advantageGrants.RemoveAll(g => g.Source == source);
        public IReadOnlyList<AdvantageGrant> GetAdvantageGrants() => _advantageGrants;
    
        // ==================== STATUS EFFECT SYSTEM ====================
    
        public event Action<int> OnDamaged;
        public event Action OnAttackMade;
        public event Action OnSkillUsed;
    
        /// <summary>Handles stacking rules and emits events. Returns null if feature requirements not met.</summary>
        public virtual AppliedEntityCondition ApplyCondition(EntityCondition effect, UnityEngine.Object applier)
        {
            return statusEffects.Apply(effect, applier);
        }
        
        public virtual void RemoveCondition(AppliedEntityCondition statusEffect)
        {
            statusEffects.Remove(statusEffect);
        }
    
        /// <summary>Removes all effects matching the specified categories (skill-based dispel).</summary>
        public virtual void RemoveConditionsByCategory(ConditionCategory categories)
        {
            statusEffects.RemoveByCategory(categories);
        }
    
        /// <summary>Removes all instances of a specific template (targeted dispel).</summary>
        public virtual void RemoveConditionsByTemplate(EntityCondition template)
        {
            statusEffects.RemoveByTemplate(template);
        }
    
        /// <summary>Processes removal triggers for all active effects. Called by phase handlers and performers.</summary>
        public virtual void NotifyConditionTrigger(RemovalTrigger trigger)
        {
            statusEffects.ProcessRemovalTrigger(trigger);
        }
    
        public virtual List<AppliedEntityCondition> GetActiveConditions()
        {
            return statusEffects.GetActive();
        }
    
        /// <summary>Ticks periodic effects, decrements durations, removes expired. Called each turn.</summary>
        public virtual void UpdateConditions()
        {
            statusEffects.OnTurnStart();
        }
    
    
        // ==================== TARGETING ====================
        
        public virtual bool CanBeTargeted()
        {
            return !isDestroyed;
        }
    
        public virtual string GetDisplayName()
        {
            return name; // GameObject name from unity editor.
        }
        
        // ==================== BASE VALUE RESOLUTION ====================
    
        /// <summary>Returns the raw base value for an attribute before modifiers. Override in subclasses to add attributes.</summary>
        public virtual int GetBaseValue(Attribute attribute)
        {
            return attribute switch
            {
                Attribute.MaxHealth => baseMaxHealth,
                Attribute.ArmorClass => baseArmorClass,
                _ => 0
            };
        }
    }
}
