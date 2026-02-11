using Assets.Scripts.Core;
using Assets.Scripts.Combat.Damage;
using System.Collections.Generic;
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
    [SerializeField]
    [Tooltip("Number of damage dice (base value before modifiers)")]
    private int baseDamageDice = 1;
    
    [SerializeField]
    [Tooltip("Size of damage dice (4, 6, 8, 10, 12) (base value before modifiers)")]
    private int baseDamageDieSize = 8;
    
    [SerializeField]
    [Tooltip("Flat damage bonus added to roll (base value before modifiers)")]
    private int baseDamageBonus = 0;
    
    [Tooltip("Type of damage this weapon deals")]
    public DamageType damageType = DamageType.Physical;
    
    [Header("Weapon Stats")]
    [SerializeField]
    [Tooltip("Attack bonus (for to-hit rolls) (base value before modifiers)")]
    private int baseAttackBonus = 0;
    
    [SerializeField]
    [Tooltip("Maximum ammunition count (-1 = unlimited) (base value before modifiers)")]
    private int baseMaxAmmo = -1;
    
    [Header("Runtime State")]
    [Tooltip("Current ammunition remaining")]
    public int currentAmmo;
    
    // ==================== STAT ACCESSORS ====================
    // Naming convention:
    // - GetBaseStat() returns raw field value (no modifiers)
    // - GetStat() returns effective value (with modifiers via StatCalculator)
    // Game code should almost always use GetStat() for gameplay calculations.
    
    // Runtime state accessor
    public int GetCurrentAmmo() => currentAmmo;
    
    
    // Base value accessors (return raw field values without modifiers)
    public int GetBaseDamageDice() => baseDamageDice;
    public int GetBaseDamageDieSize() => baseDamageDieSize;
    public int GetBaseDamageBonus() => baseDamageBonus;
    public int GetBaseAttackBonus() => baseAttackBonus;
    public int GetBaseMaxAmmo() => baseMaxAmmo;
    
    // Modified value accessors (return values with all modifiers applied via StatCalculator)
    // INTEGER-FIRST: StatCalculator returns int, no rounding needed
    public int GetDamageDice() => StatCalculator.GatherAttributeValue(this, Attribute.DamageDice, baseDamageDice);
    public int GetDamageDieSize() => StatCalculator.GatherAttributeValue(this, Attribute.DamageDieSize, baseDamageDieSize);
    public int GetDamageBonus() => StatCalculator.GatherAttributeValue(this, Attribute.DamageBonus, baseDamageBonus);
    public int GetAttackBonus() => StatCalculator.GatherAttributeValue(this, Attribute.AttackBonus, baseAttackBonus);
    public int GetMaxAmmo() => StatCalculator.GatherAttributeValue(this, Attribute.Ammo, baseMaxAmmo);
    
    /// <summary>
    /// Get damage string for display (e.g., "1d8+2 Physical")
    /// Uses modified values (with bonuses applied).
    /// </summary>
    public string DamageString
    {
        get
        {
            int dice = GetDamageDice();
            int dieSize = GetDamageDieSize();
            int bonus = GetDamageBonus();
            string bonusStr = bonus != 0 ? $"{bonus:+0;-0}" : "";
            return $"{dice}d{dieSize}{bonusStr} {damageType}";
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
        baseMaxHealth = 40;      // Somewhat fragile
        health = 40;         // Start at full HP
        baseArmorClass = 14;     // Exposed, easier to hit
        baseComponentSpace = 150;  // Consumes component space
        basePowerDrawPerTurn = 5;  // Requires power to stay armed
        
        // Set weapon damage defaults
        baseDamageDice = 1;
        baseDamageDieSize = 8;
        baseDamageBonus = 0;
        damageType = DamageType.Physical;
        baseAttackBonus = 0;
        
        // Each weapon ENABLES ONE "Gunner" role slot
        roleType = RoleType.Gunner;
    }
    
    void Awake()
    {
        // Set component type (in case Reset wasn't called)
        componentType = ComponentType.Weapon;
        
        // Each weapon ENABLES ONE "Gunner" role slot
        roleType = RoleType.Gunner;
        
        // Initialize ammo to max (using accessor for modified value)
        currentAmmo = GetMaxAmmo();
    }
    
    /// <summary>
    /// Get the stats to display in the UI for this weapon.
    /// Uses StatCalculator for modified values.
    /// INTEGER-FIRST: All stats are integers.
    /// </summary>
    public override List<VehicleComponentUI.DisplayStat> GetDisplayStats()
    {
        var stats = new List<VehicleComponentUI.DisplayStat>();
        
        // Get modified values using accessor methods (all integers)
        int modifiedDamageDice = GetDamageDice();
        int modifiedDamageDieSize = GetDamageDieSize();
        int modifiedDamageBonus = GetDamageBonus();
        int modifiedAttackBonus = GetAttackBonus();
        
        // Damage dice with bonus
        string dmgStr = modifiedDamageBonus != 0 
            ? $"{modifiedDamageDice}d{modifiedDamageDieSize}{modifiedDamageBonus:+0;-0}"
            : $"{modifiedDamageDice}d{modifiedDamageDieSize}";
        stats.Add(VehicleComponentUI.DisplayStat.WithTooltip("Damage", "DMG", Attribute.DamageBonus, baseDamageBonus, modifiedDamageBonus, $" ({dmgStr})"));
        
        // Attack bonus
        string attackStr = modifiedAttackBonus >= 0 ? $"+{modifiedAttackBonus}" : $"{modifiedAttackBonus}";
        stats.Add(VehicleComponentUI.DisplayStat.WithTooltip("Attack", "HIT", Attribute.AttackBonus, baseAttackBonus, modifiedAttackBonus, attackStr));
        
        // Ammo if not unlimited
        if (baseMaxAmmo != -1)
        {
            int modifiedMaxAmmo = GetMaxAmmo();
            stats.Add(VehicleComponentUI.DisplayStat.BarWithTooltip("Ammo", "AMMO", Attribute.Ammo, currentAmmo, baseMaxAmmo, modifiedMaxAmmo));
        }
        
        // Add base class stats (power draw)
        stats.AddRange(base.GetDisplayStats());
        
        return stats;
    }
}
