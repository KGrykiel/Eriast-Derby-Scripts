using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Entities;
using Assets.Scripts.StatusEffects;
using Assets.Scripts.Combat;

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
    
    [Tooltip("Maximum health points")]
    public int maxHealth = 100;
    
    [Tooltip("Armor class (difficulty to hit)")]
    public int armorClass = 10;
    
    [Header("Entity State")]
    [Tooltip("Is this entity destroyed?")]
    public bool isDestroyed = false;
    
    [Header("Entity Features")]
    [Tooltip("Capability flags for status effect targeting validation")]
    public EntityFeature features = EntityFeature.None;
    
    [Header("Damage Resistances")]
    [Tooltip("Resistances and vulnerabilities to different damage types")]
    public List<DamageResistance> resistances = new List<DamageResistance>();

    // ==================== MODIFIER & STATUS EFFECT STORAGE ====================
    
    [SerializeField, HideInInspector]
    protected List<AttributeModifier> entityModifiers = new List<AttributeModifier>();
    
    [SerializeField, HideInInspector]
    protected List<AppliedStatusEffect> activeStatusEffects = new List<AppliedStatusEffect>();

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

    // ==================== ARMOR CLASS ====================
    
    /// <summary>
    /// Get armor class for targeting calculations.
    /// Override for dynamic AC (modifiers, cover, etc.)
    /// </summary>
    public virtual int GetArmorClass()
    {
        return armorClass;
    }
    
    // ==================== DAMAGE RESISTANCES ====================
    
    /// <summary>
    /// Get resistance level for a specific damage type.
    /// Returns Normal if no resistance is defined.
    /// </summary>
    public virtual ResistanceLevel GetResistance(DamageType type)
    {
        var resistance = resistances.FirstOrDefault(r => r.type == type);
        return resistance.level != default ? resistance.level : ResistanceLevel.Normal;
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
        
        health = Mathf.Min(health + amount, maxHealth);
    }

    /// <summary>
    /// Get health as a percentage (0-1).
    /// </summary>
    public float GetHealthPercent()
    {
        if (maxHealth <= 0) return 0f;
        return (float)health / maxHealth;
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
    
    /// <summary>
    /// Apply modifiers to a base attribute value.
    /// Returns the modified value after applying all relevant modifiers.
    /// 
    /// Application order follows D&D standard:
    /// 1. Apply all Flat modifiers (additive)
    /// 2. Apply all Multiplier modifiers (multiplicative)
    /// </summary>
    protected float ApplyModifiers(Attribute attr, float baseValue)
    {
        float result = baseValue;
        
        // Step 1: Apply all flat modifiers (additive)
        foreach (var mod in entityModifiers)
        {
            if (mod.Attribute != attr) continue;
            
            if (mod.Type == ModifierType.Flat)
                result += mod.Value;
        }
        
        // Step 2: Apply all multiplier modifiers (multiplicative)
        foreach (var mod in entityModifiers)
        {
            if (mod.Attribute != attr) continue;
            
            if (mod.Type == ModifierType.Multiplier)
                result *= mod.Value;
        }

        return result;
    }

    // ==================== STATUS EFFECT SYSTEM ====================
    
    /// <summary>
    /// Apply a status effect to this entity.
    /// Handles stacking rules: same status effect compares and keeps better one.
    /// Emits StatusEffectEvent for logging via CombatEventBus.
    /// Returns the applied (or existing better) status effect instance, or null if failed.
    /// </summary>
    public virtual AppliedStatusEffect ApplyStatusEffect(StatusEffect effect, UnityEngine.Object applier)
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
    /// Remove all status effects from a specific source.
    /// </summary>
    public virtual void RemoveStatusEffectsFromSource(UnityEngine.Object source)
    {
        var toRemove = activeStatusEffects.Where(s => s.applier == source).ToList();
        
        foreach (var statusEffect in toRemove)
        {
            RemoveStatusEffect(statusEffect);
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
}
