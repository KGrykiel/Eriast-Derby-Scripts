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
    [Header("Weapon Stats")]
    [Tooltip("Base damage of this weapon")]
    public int baseDamage = 10;
    
    [Tooltip("Effective range in meters")]
    public int range = 100;
    
    [Tooltip("Ammunition count (-1 = unlimited)")]
    public int ammo = -1;
    
    [Tooltip("Current ammunition remaining")]
    public int currentAmmo;
    
    void Awake()
    {
        // Set component type
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
                Debug.LogWarning($"[Weapon] {componentName} is out of ammo!");
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
        
        currentAmmo = Mathf.Min(currentAmmo + amount, ammo);
        Debug.Log($"[Weapon] {componentName} reloaded to {currentAmmo}/{ammo} ammo");
    }
    
    /// <summary>
    /// Called when weapon is destroyed.
    /// Vehicle loses one Gunner role slot.
    /// </summary>
    protected override void OnComponentDestroyed()
    {
        base.OnComponentDestroyed();
        
        // Weapon destruction disables one Gunner slot
        Debug.LogWarning($"[Weapon] {componentName} destroyed! One Gunner role slot lost!");
        
        // The base class already logs that the "Gunner" role is no longer available
    }
    
    /// <summary>
    /// Get status summary including ammo count.
    /// </summary>
    public override string GetStatusSummary()
    {
        string status = base.GetStatusSummary();
        
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
