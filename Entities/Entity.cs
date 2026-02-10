using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.StatusEffects;
using Assets.Scripts.Entities;
using Assets.Scripts.Entities.Vehicle;
using Assets.Scripts.Combat;
using Assets.Scripts.Core;

/// <summary>
/// Abstract base class for all entities that can be damaged, targeted, or interact with skills.
/// Entities are "things with HP" - this includes:
/// - VehicleComponents (chassis, weapons, power cores, etc.)
/// - Props (barrels, doors, obstacles)
/// - NPCs (creatures, turrets)
/// - Any other targetable object
/// 
/// NOTE: Vehicle itself is NOT an Entity - it's a container/coordinator for Entity components.
/// 
/// Display name uses Unity's built-in name property (GameObject.name).
/// 
/// MODIFIER SYSTEM (Phase 1):
/// - Entities store their own modifiers and status effects
/// - Two sources: StatusEffect (temporary/indefinite) OR Component (permanent equipment)
/// - Skills apply StatusEffects ONLY, Components apply direct modifiers
/// 
/// LOGGING: Status effect events are emitted to CombatEventBus for aggregated logging.
/// </summary>
public abstract class Entity : MonoBehaviour
{
    [Header("Entity Stats")]
    [Tooltip("Current health points")]
    public int health = 100;
    
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
    public virtual int GetMaxHealth() => StatCalculator.GatherAttributeValue(this, Attribute.MaxHealth, baseMaxHealth);
    public virtual int GetArmorClass() => StatCalculator.GatherAttributeValue(this, Attribute.ArmorClass, baseArmorClass);
    
    // ==================== ENTITY FEATURES ====================
    
    /// <summary>
    /// Check if this entity has a specific feature (or combination of features).
    /// </summary>
    public bool HasFeature(EntityFeature feature)
    {
        return (features & feature) == feature;
    }
    
    /// <summary>
    /// Check if entity has ANY of the specified features.
    /// </summary>
    public bool HasAnyFeature(EntityFeature flags)
    {
        return (features & flags) != 0;
    }

    // ==================== DAMAGE RESISTANCES ====================
    
    /// <summary>
    /// Get resistance level for a specific damage type.
    /// Returns Normal if no resistance is defined.
    /// </summary>
    public virtual ResistanceLevel GetResistance(DamageType type)
    {
        // Find resistance entry for this damage type
        foreach (var resistance in resistances)
        {
            if (resistance.type == type)
                return resistance.level;
        }
        
        // No resistance defined - return normal
        return ResistanceLevel.Normal;
    }

    // ==================== DAMAGE & HEALING ====================
    
    /// <summary>
    /// Apply damage to this entity.
    /// Override in subclasses for custom damage handling (resistances, shields, etc.)
    /// </summary>
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

    /// <summary>
    /// Called when damage is taken. Override for logging, effects, etc.
    /// </summary>
    protected virtual void OnDamageTaken(int amount, int previousHealth, int newHealth)
    {
        // Override in subclasses
    }

    /// <summary>
    /// Called when entity is destroyed (health reaches 0).
    /// Override in subclasses for destruction effects, drops, etc.
    /// </summary>
    protected virtual void OnEntityDestroyed()
    {
        // Override in subclasses
    }

    /// <summary>
    /// Heal this entity by the specified amount.
    /// </summary>
    public virtual void Heal(int amount)
    {
        if (isDestroyed) return;
        
        health = Mathf.Min(health + amount, GetMaxHealth());
    }

    // ==================== MODIFIER SYSTEM ====================
    
    /// <summary>
    /// Add a direct modifier to this entity.
    /// Used by Components to apply permanent equipment bonuses.
    /// Skills should use ApplyStatusEffect() instead.
    /// </summary>
    public virtual void AddModifier(AttributeModifier modifier)
    {
        entityModifiers.Add(modifier);
    }
    
    /// <summary>
    /// Remove a specific modifier from this entity.
    /// </summary>
    public virtual void RemoveModifier(AttributeModifier modifier)
    {
        entityModifiers.Remove(modifier);
    }
    
    /// <summary>
    /// Get all active modifiers on this entity.
    /// </summary>
    public virtual List<AttributeModifier> GetModifiers()
    {
        return entityModifiers;
    }

    // ==================== STATUS EFFECT SYSTEM ====================
    
    /// <summary>
    /// Apply a status effect to this entity.
    /// Handles stacking rules: same status effect compares and keeps better one.
    /// Emits StatusEffectEvent for logging via CombatEventBus.
    /// Returns the applied (or existing better) status effect instance, or null if failed.
    /// </summary>
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
    
    /// <summary>
    /// Remove a specific status effect from this entity.
    /// </summary>
    public virtual void RemoveStatusEffect(AppliedStatusEffect statusEffect)
    {
        if (activeStatusEffects.Remove(statusEffect))
        {
            statusEffect.OnRemove();
        }
    }
    
    /// <summary>
    /// Remove all status effects applied by a specific source.
    /// Much more efficient than searching by template.
    /// Useful for removing stage/lane effects when leaving.
    /// </summary>
    public virtual void RemoveStatusEffectsFromSource(Object source)
    {
        if (source == null) return;
        
        var toRemove = activeStatusEffects.Where(e => e.applier == source).ToList();
        foreach (var effect in toRemove)
        {
            RemoveStatusEffect(effect);
        }
    }
    
    /// <summary>
    /// Get all active status effects on this entity.
    /// </summary>
    public virtual List<AppliedStatusEffect> GetActiveStatusEffects()
    {
        return activeStatusEffects;
    }
    
    /// <summary>
    /// Update status effects (tick periodic effects, decrement durations, remove expired).
    /// Call this at the start/end of each turn.
    /// </summary>
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
    
    /// <summary>
    /// Check if a status effect can be applied to this entity (feature validation).
    /// </summary>
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
    
    /// <summary>
    /// Determine if new status effect should replace existing one (stacking rules).
    /// </summary>
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
    
    /// <summary>
    /// Check if this entity can be targeted.
    /// Override for invisibility, phasing, etc.
    /// </summary>
    public virtual bool CanBeTargeted()
    {
        return !isDestroyed;
    }

    /// <summary>
    /// Get display name for UI.
    /// Uses Unity's GameObject.name by default.
    /// Override if you need custom display logic.
    /// </summary>
    public virtual string GetDisplayName()
    {
        return name; // Unity's built-in Object.name property
    }
    
    // ==================== D20 ROLL BASE VALUES ====================
    
    /// <summary>
    /// Get the base value this component provides for a d20 check or save.
    /// 
    /// This is used by D20RollHelpers to gather component bonuses for checks and saves.
    /// Components override this to provide their base check values (e.g., Chassis provides Mobility).
    /// 
    /// Uses VehicleCheckAttribute (constrained subset) instead of full Attribute enum for type safety.
    /// Only valid check attributes (Mobility, Stability) can be rolled - prevents rolling MaxHealth or DamageDice.
    /// 
    /// Returns 0 by default (most entities don't contribute to d20 rolls).
    /// 
    /// NOTE: This is the BASE value only, not including applied modifiers (status effects, equipment).
    /// Applied modifiers are gathered separately via StatCalculator.
    /// </summary>
    /// <param name="checkAttribute">The check attribute being rolled (Mobility, Stability, etc.)</param>
    /// <returns>Base value contribution, or 0 if this component doesn't provide this check attribute</returns>
    public virtual int GetBaseCheckValue(VehicleCheckAttribute checkAttribute)
    {
        return 0;
    }
}
