using UnityEngine;

// Abstract base class for all entities that can be the source or target of effects
public abstract class Entity : MonoBehaviour
{
    public string id;

    [Header("Core Stats")]
    [HideInInspector] public int health = 100;
    public int maxHealth = 100;

    [Tooltip("The base armor class for this entity.")]
    public int armorClass = 10;

    public virtual int GetArmorClass()
    {
        return armorClass;
    }

    // Entity.cs
    public virtual void TakeDamage(int amount)
    {
        health = Mathf.Max(health - amount, 0);

        if (health <= 0)
        {
            OnEntityDestroyed();
        }
    }

    protected virtual void OnEntityDestroyed()
    {
        // Base implementation (override in subclasses)
    }
}
