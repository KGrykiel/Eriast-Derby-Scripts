using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// Weapon component - a damage-dealing system on the vehicle.
/// OPTIONAL: Vehicles can have 0 to many weapons.
/// Each weapon ENABLES ONE "Gunner" role slot (one character per weapon).
/// Provides: Damage stats and weapon-specific skills.
/// </summary>
public class WeaponComponent : VehicleComponent
{
    [Header("Weapon Damage (D&D Style)")]
    [Tooltip("Number of damage dice")]
    public int damageDice = 1;
    
    [Tooltip("Size of damage dice (4, 6, 8, 10, 12)")]
    public int damageDieSize = 8;
    
    [Tooltip("Flat damage bonus added to roll")]
    public int damageBonus = 0;
    
    [Tooltip("Type of damage this weapon deals")]
    public DamageType damageType = DamageType.Physical;
    
    [Header("Weapon Stats")]
    [Tooltip("Attack bonus (for to-hit rolls)")]
    public int attackBonus = 0;
    
    [Tooltip("Ammunition count (-1 = unlimited)")]
    public int ammo = -1;
    
    [Tooltip("Current ammunition remaining")]
    public int currentAmmo;
    
    /// <summary>
    /// Get damage string for display (e.g., "1d8+2 Physical")
    /// </summary>
    public string DamageString
    {
        get
        {
            string bonus = damageBonus != 0 ? $"+{damageBonus}" : "";
            return $"{damageDice}d{damageDieSize}{bonus} {damageType}";
        }
    }
    
    /// <summary>
    /// Roll this weapon's damage dice.
    /// </summary>
    public int RollDamage()
    {
        return RollUtility.RollDamage(damageDice, damageDieSize, damageBonus);
    }
    
    /// <summary>
    /// Create a damage packet from this weapon's damage.
    /// </summary>
    public DamagePacket CreateDamagePacket(Entity source, bool isCritical = false)
    {
        int damage = RollDamage();
        
        // Critical hits double the dice (roll again and add)
        if (isCritical)
        {
            damage += RollUtility.RollDamage(damageDice, damageDieSize, 0); // No bonus on crit dice
        }
        
        return DamagePacket.FromWeapon(damage, damageType, source, isCritical);
    }
    
    /// <summary>
    /// Called when component is first added or reset in Editor.
    /// Sets default values that appear immediately in Inspector.
    /// </summary>
    void Reset()
    {
        // Set GameObject name (shows in hierarchy)
        gameObject.name = "Weapon";
        
        // Set component identity
        componentType = ComponentType.Weapon;
        
        // Set component base stats using Entity fields
        maxHealth = 40;      // Somewhat fragile
        health = 40;         // Start at full HP
        armorClass = 14;     // Exposed, easier to hit
        componentSpace = 150;  // Consumes component space
        powerDrawPerTurn = 5;  // Requires power to stay armed
        
        // Set weapon damage defaults
        damageDice = 1;
        damageDieSize = 8;
        damageBonus = 0;
        damageType = DamageType.Physical;
        attackBonus = 0;
        
        // Each weapon ENABLES ONE "Gunner" role slot
        enablesRole = true;
        roleName = "Gunner";
    }
    
    void Awake()
    {
        // Set component type (in case Reset wasn't called)
        componentType = ComponentType.Weapon;
        
        // Each weapon ENABLES ONE "Gunner" role slot
        enablesRole = true;
        roleName = "Gunner";
        
        // Initialize ammo
        currentAmmo = ammo;
    }
    
    /// <summary>
    /// Weapons typically don't provide vehicle-level stat bonuses.
    /// They provide combat capabilities through skills instead.
    /// Override this if a weapon provides passive bonuses (e.g., defensive turret adds AC).
    /// </summary>
    public override VehicleStatModifiers GetStatModifiers()
    {
        // If weapon is destroyed or disabled, it contributes nothing
        if (isDestroyed || isDisabled)
            return VehicleStatModifiers.Zero;
        
        // Most weapons don't provide passive stat bonuses
        // They provide combat power through componentSkills instead
        // Override in subclasses if needed (e.g., shield weapons might add AC)
        return VehicleStatModifiers.Zero;
    }
    
    /// <summary>
    /// Check if this weapon has ammunition available.
    /// Returns true if unlimited ammo or ammo remaining.
    /// </summary>
    public bool HasAmmo()
    {
        return ammo == -1 || currentAmmo > 0;
    }
    
    /// <summary>
    /// Consume one unit of ammunition.
    /// Call this when the weapon is fired.
    /// </summary>
    public void ConsumeAmmo()
    {
        if (ammo != -1 && currentAmmo > 0)
        {
            currentAmmo--;
            
            if (currentAmmo == 0)
            {
                Debug.LogWarning($"[Weapon] {name} is out of ammo!");
            }
        }
    }
    
    /// <summary>
    /// Reload/restock ammunition.
    /// Can be called by Engineer or during pit stops.
    /// </summary>
    public void Reload(int amount)
    {
        if (ammo == -1) return; // Unlimited ammo, can't reload
        
        currentAmmo = Math.Min(currentAmmo + amount, ammo);
        Debug.Log($"[Weapon] {name} reloaded to {currentAmmo}/{ammo} ammo");
    }
    
    /// <summary>
    /// Called when weapon is destroyed.
    /// Vehicle loses one Gunner role slot.
    /// </summary>
    protected override void OnComponentDestroyed()
    {
        base.OnComponentDestroyed();
        
        // Weapon destruction disables one Gunner slot
        Debug.LogWarning($"[Weapon] {name} destroyed! One Gunner role slot lost!");
        
        // The base class already logs that the "Gunner" role is no longer available
    }
    
    /// <summary>
    /// Get status summary including ammo count and damage.
    /// </summary>
    public override string GetStatusSummary()
    {
        string status = base.GetStatusSummary();
        
        // Add damage info
        status += $"Damage: {DamageString}\n";
        
        // Add ammo info if weapon has limited ammo
        if (ammo != -1)
        {
            status += $"Ammo: {currentAmmo}/{ammo}\n";
        }
        else
        {
            status += "Ammo: Unlimited\n";
        }
        
        return status;
    }
}
