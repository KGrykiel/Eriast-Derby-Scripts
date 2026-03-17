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

    private EntityStatusEffectManager statusEffects;

    protected virtual void Awake()
    {
        statusEffects = new EntityStatusEffectManager(this);
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

        if (health <= 0 && !isDestroyed)
        {
            isDestroyed = true;
            OnEntityDestroyed();
        }
    }

    protected virtual void OnDamageTaken(int amount, int previousHealth, int newHealth){}

    protected virtual void OnEntityDestroyed(){}

    protected virtual void OnDestroy()
    {
        statusEffects?.Cleanup();
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
        return statusEffects.Apply(effect, applier);
    }
    
    public virtual void RemoveStatusEffect(AppliedStatusEffect statusEffect)
    {
        statusEffects.Remove(statusEffect);
    }

    /// <summary>Removes all effects from a specific source (e.g. leaving a lane).</summary>
    public virtual void RemoveStatusEffectsFromSource(Object source)
    {
        statusEffects.RemoveFromSource(source);
    }
    
    public virtual List<AppliedStatusEffect> GetActiveStatusEffects()
    {
        return statusEffects.GetActive();
    }

    /// <summary>Ticks periodic effects, decrements durations, removes expired. Called each turn.</summary>
    public virtual void UpdateStatusEffects()
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
