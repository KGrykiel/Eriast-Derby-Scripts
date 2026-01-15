using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// Weapon component - a damage-dealing system on the vehicle.
/// OPTIONAL: Vehicles can have 0 to many weapons.
/// Each weapon ENABLES ONE "Gunner" role slot (one character per weapon).
/// Provides: Damage stats and weapon-specific skills.
/// 
/// NOTE: Weapon damage is rolled via DamageFormula, not directly on this component.
/// This component stores the weapon's damage STATS (dice, type, bonus).
/// DamageFormula reads these stats and handles actual rolling.
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
    /// Get attack bonus (with modifiers applied).
    /// NOTE: For display purposes. Use StatCalculator for authoritative values.
    /// </summary>
    public int GetAttackBonus()
    {
        return attackBonus; // Base value - StatCalculator handles modifiers
    }
    
    /// <summary>
    /// Get damage bonus (with modifiers applied).
    /// NOTE: For display purposes. Use StatCalculator for authoritative values.
    /// </summary>
    public int GetDamageBonus()
    {
        return damageBonus; // Base value - StatCalculator handles modifiers
    }
    
    /// <summary>
    /// Get max ammo.
    /// NOTE: Ammo modifiers would be handled by StatCalculator if needed.
    /// </summary>
    public int GetMaxAmmo()
    {
        return ammo;
    }
    
    /// <summary>
    /// Get damage string for display (e.g., "1d8+2 Physical")
    /// </summary>
    public string DamageString
    {
        get
        {
            string bonus = damageBonus != 0 ? $"{damageBonus:+0;-0}" : "";
            return $"{damageDice}d{damageDieSize}{bonus} {damageType}";
        }
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
        roleType = RoleType.Gunner;
    }
    
    void Awake()
    {
        // Set component type (in case Reset wasn't called)
        componentType = ComponentType.Weapon;
        
        // Each weapon ENABLES ONE "Gunner" role slot
        enablesRole = true;
        roleType = RoleType.Gunner;
        
        // Initialize ammo
        currentAmmo = ammo;
    }
    
    /// <summary>
    /// Get the stats to display in the UI for this weapon.
    /// Uses StatCalculator for modified values.
    /// </summary>
    public override List<VehicleComponentUI.DisplayStat> GetDisplayStats()
    {
        var stats = new List<VehicleComponentUI.DisplayStat>();
        
        // Get modified values from StatCalculator
        float modifiedDamageBonus = Core.StatCalculator.GatherAttributeValue(this, Attribute.DamageBonus, damageBonus);
        float modifiedAttackBonus = Core.StatCalculator.GatherAttributeValue(this, Attribute.AttackBonus, attackBonus);
        
        // Damage dice with bonus
        string dmgStr = modifiedDamageBonus != 0 
            ? $"{damageDice}d{damageDieSize}{modifiedDamageBonus:+0;-0}"
            : $"{damageDice}d{damageDieSize}";
        stats.Add(VehicleComponentUI.DisplayStat.WithTooltip("Damage", "DMG", Attribute.DamageBonus, damageBonus, modifiedDamageBonus, $" ({dmgStr})"));
        
        // Attack bonus
        string attackStr = modifiedAttackBonus >= 0 ? $"+{modifiedAttackBonus:F0}" : $"{modifiedAttackBonus:F0}";
        stats.Add(VehicleComponentUI.DisplayStat.WithTooltip("Attack", "HIT", Attribute.AttackBonus, attackBonus, modifiedAttackBonus, attackStr));
        
        // Ammo if not unlimited
        if (ammo != -1)
        {
            float modifiedMaxAmmo = Core.StatCalculator.GatherAttributeValue(this, Attribute.Ammo, ammo);
            stats.Add(VehicleComponentUI.DisplayStat.BarWithTooltip("Ammo", "AMMO", Attribute.Ammo, currentAmmo, ammo, modifiedMaxAmmo));
        }
        
        // Add base class stats (power draw)
        stats.AddRange(base.GetDisplayStats());
        
        return stats;
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
    public string GetStatusSummary()
    {
        string status = VehicleComponentUI.GetStatusSummary(this);
        
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
