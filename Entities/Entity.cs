using UnityEngine;
using System.Collections.Generic;
using Assets.Scripts.Combat.Damage;
using System.Linq;
using Assets.Scripts.StatusEffects;
using Assets.Scripts.Entities;
using Assets.Scripts.Combat;
using Assets.Scripts.Core;

/// <summary>
/// Base class for anything with HP that can be damaged/targeted.
/// Note: Vehicle itself is NOT an Entity, it's a container for Entity components.
/// </summary>
public abstract class Entity : MonoBehaviour
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
    public bool isDestroyed = false;
    
    [Header("Entity Features")]
    [Tooltip("Capability flags for status effect targeting validation")]
    public EntityFeature features = EntityFeature.None;
    
    [Header("Damage Resistances")]
    [Tooltip("Resistances and vulnerabilities to different damage types")]
    public List<DamageResistance> resistances = new();

    // ==================== MODIFIER & STATUS EFFECT STORAGE ====================
    
    [SerializeField, HideInInspector]
    protected List<AttributeModifier> entityModifiers = new();
    
    [SerializeField, HideInInspector]
    protected List<AppliedStatusEffect> activeStatusEffects = new();

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

        if (health <= 0 && !isDestroyed)
        {
            isDestroyed = true;
            OnEntityDestroyed();
        }
    }

    protected virtual void OnDamageTaken(int amount, int previousHealth, int newHealth){}

    protected virtual void OnEntityDestroyed(){}

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

    public virtual List<AttributeModifier> GetModifiers()
    {
        return entityModifiers;
    }

    // ==================== STATUS EFFECT SYSTEM ====================
    
    /// <summary>Handles stacking rules and emits events. Returns null if feature requirements not met.</summary>
    public virtual AppliedStatusEffect ApplyStatusEffect(StatusEffect effect, Object applier)
    {
        // Validate targeting (feature requirements)
        if (!CanApplyStatusEffect(effect))
        {
            Debug.LogWarning($"[Entity] Cannot apply {effect.effectName} to {GetDisplayName()} - feature requirements not met");
            return null;
        }
        
        // Check for existing status effect of same type (stacking rules)
        var existing = activeStatusEffects.FirstOrDefault(a => a.template == effect);
        bool wasReplacement = false;
        
        if (existing != null)
        {
            // Compare and keep better effect
            if (ShouldReplaceStatusEffect(existing, effect))
            {
                // Remove old effect
                existing.OnRemove();
                activeStatusEffects.Remove(existing);
                wasReplacement = true;
            }
            else
            {
                // Keep existing effect (it's better or equal)
                return existing;
            }
        }
        
        // Create and apply new status effect
        var applied = new AppliedStatusEffect(effect, this, applier);
        applied.OnApply();
        activeStatusEffects.Add(applied);
        
        // Emit event for logging (CombatEventBus handles aggregation)
        Entity sourceEntity = applier as Entity;
        CombatEventBus.EmitStatusEffect(applied, sourceEntity, this, applier, wasReplacement);
        
        return applied;
    }
    
    public virtual void RemoveStatusEffect(AppliedStatusEffect statusEffect)
    {
        if (activeStatusEffects.Remove(statusEffect))
        {
            statusEffect.OnRemove();
        }
    }

    /// <summary>Removes all effects from a specific source (e.g. leaving a lane).</summary>
    public virtual void RemoveStatusEffectsFromSource(Object source)
    {
        if (source == null) return;
        
        var toRemove = activeStatusEffects.Where(e => e.applier == source).ToList();
        foreach (var effect in toRemove)
        {
            RemoveStatusEffect(effect);
        }
    }
    
    public virtual List<AppliedStatusEffect> GetActiveStatusEffects()
    {
        return activeStatusEffects;
    }

    /// <summary>Ticks periodic effects, decrements durations, removes expired. Called each turn.</summary>
    public virtual void UpdateStatusEffects()
    {
        // Tick all status effects (periodic damage, healing, etc.)
        foreach (var statusEffect in activeStatusEffects.ToList())
        {
            statusEffect.OnTick();
        }
        
        // Decrement durations and remove expired
        for (int i = activeStatusEffects.Count - 1; i >= 0; i--)
        {
            var statusEffect = activeStatusEffects[i];
            
            statusEffect.DecrementDuration();
            
            if (statusEffect.IsExpired)
            {
                // Emit expiration event
                CombatEventBus.EmitStatusExpired(statusEffect, this);
                
                statusEffect.OnRemove();
                activeStatusEffects.RemoveAt(i);
            }
        }
    }
    
    public virtual bool CanApplyStatusEffect(StatusEffect effect)
    {
        // Check required features
        if (effect.requiredFeatures != EntityFeature.None)
        {
            if (!HasFeature(effect.requiredFeatures))
                return false;
        }
        
        // Check excluded features
        if (effect.excludedFeatures != EntityFeature.None)
        {
            if (HasAnyFeature(effect.excludedFeatures))
                return false;
        }
        
        return true;
    }
    
    /// <summary>Stacking rules: higher magnitude wins, then longer duration.</summary>
    private bool ShouldReplaceStatusEffect(AppliedStatusEffect existing, StatusEffect newEffect)
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
