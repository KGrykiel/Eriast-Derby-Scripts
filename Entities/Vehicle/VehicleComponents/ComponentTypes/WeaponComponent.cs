using Assets.Scripts.Core;
using Assets.Scripts.Combat.Damage;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Damage-dealing system. enables Gunner role. Can have multiple per vehicle.</summary>
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

    public int GetCurrentAmmo() => currentAmmo;

    public int GetBaseDamageDice() => baseDamageDice;
    public int GetBaseDamageDieSize() => baseDamageDieSize;
    public int GetBaseDamageBonus() => baseDamageBonus;
    public int GetBaseAttackBonus() => baseAttackBonus;
    public int GetBaseMaxAmmo() => baseMaxAmmo;

    public int GetDamageDice() => StatCalculator.GatherAttributeValue(this, Attribute.DamageDice, baseDamageDice);
    public int GetDamageDieSize() => StatCalculator.GatherAttributeValue(this, Attribute.DamageDieSize, baseDamageDieSize);
    public int GetDamageBonus() => StatCalculator.GatherAttributeValue(this, Attribute.DamageBonus, baseDamageBonus);
    public int GetAttackBonus() => StatCalculator.GatherAttributeValue(this, Attribute.AttackBonus, baseAttackBonus);
    public int GetMaxAmmo() => StatCalculator.GatherAttributeValue(this, Attribute.Ammo, baseMaxAmmo);

    /// <summary>
    /// Default values for convenience, to be edited manually.
    /// </summary>
    void Reset()
    {
        gameObject.name = "Weapon";
        componentType = ComponentType.Weapon;

        baseMaxHealth = 40;
        health = 40;
        baseArmorClass = 14;
        baseComponentSpace = 150;
        basePowerDrawPerTurn = 5;

        baseDamageDice = 1;
        baseDamageDieSize = 8;
        baseDamageBonus = 0;
        damageType = DamageType.Physical;
        baseAttackBonus = 0;
        roleType = RoleType.Gunner;
    }

    void Awake()
    {
        componentType = ComponentType.Weapon;
        roleType = RoleType.Gunner;
        currentAmmo = GetMaxAmmo();
    }

    public override List<VehicleComponentUI.DisplayStat> GetDisplayStats()
    {
        var stats = new List<VehicleComponentUI.DisplayStat>();

        int modifiedDamageDice = GetDamageDice();
        int modifiedDamageDieSize = GetDamageDieSize();
        int modifiedDamageBonus = GetDamageBonus();
        int modifiedAttackBonus = GetAttackBonus();

        string dmgStr = modifiedDamageBonus != 0 
            ? $"{modifiedDamageDice}d{modifiedDamageDieSize}{modifiedDamageBonus:+0;-0}"
            : $"{modifiedDamageDice}d{modifiedDamageDieSize}";
        stats.Add(VehicleComponentUI.DisplayStat.WithTooltip("Damage", "DMG", Attribute.DamageBonus, baseDamageBonus, modifiedDamageBonus, $" ({dmgStr})"));

        string attackStr = modifiedAttackBonus >= 0 ? $"+{modifiedAttackBonus}" : $"{modifiedAttackBonus}";
        stats.Add(VehicleComponentUI.DisplayStat.WithTooltip("Attack", "HIT", Attribute.AttackBonus, baseAttackBonus, modifiedAttackBonus, attackStr));

        if (baseMaxAmmo != -1)
        {
            int modifiedMaxAmmo = GetMaxAmmo();
            stats.Add(VehicleComponentUI.DisplayStat.BarWithTooltip("Ammo", "AMMO", Attribute.Ammo, currentAmmo, baseMaxAmmo, modifiedMaxAmmo));
        }
        
        stats.AddRange(base.GetDisplayStats());
        
        return stats;
    }
}
