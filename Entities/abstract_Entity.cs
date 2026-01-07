using UnityEngine;
using System.Collections.Generic;
using System.Linq;

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
    
    [Header("Damage Resistances")]
    [Tooltip("Resistances and vulnerabilities to different damage types")]
    public List<DamageResistance> resistances = new List<DamageResistance>();

    /// <summary>
    /// Get armor class for targeting calculations.
    /// Override for dynamic AC (modifiers, cover, etc.)
    /// </summary>
    public virtual int GetArmorClass()
    {
        return armorClass;
    }
    
    /// <summary>
    /// Get resistance level for a specific damage type.
    /// Returns Normal if no resistance is defined.
    /// </summary>
    public virtual ResistanceLevel GetResistance(DamageType type)
    {
        var resistance = resistances.FirstOrDefault(r => r.type == type);
        return resistance.level != default ? resistance.level : ResistanceLevel.Normal;
    }

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
