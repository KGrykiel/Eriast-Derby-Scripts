using Assets.Scripts.Core;
using Assets.Scripts.Combat.Damage;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Damage-dealing system. enables Gunner role. Can have multiple per vehicle.</summary>
public class WeaponComponent : VehicleComponent
{
    [Header("Weapon Damage")]
    [Tooltip("Base damage formula (dice, bonus, damage type)")]
    public DamageFormula baseDamageFormula = new() { baseDice = 1, dieSize = 8, bonus = 0, damageType = DamageType.Physical };
    
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
    public int GetBaseAttackBonus() => baseAttackBonus;
    public int GetBaseMaxAmmo() => baseMaxAmmo;

    public DamageFormula GetDamageFormula() => baseDamageFormula;
    public int GetAttackBonus() => StatCalculator.GatherAttributeValue(this, Attribute.AttackBonus);
    public int GetMaxAmmo() => StatCalculator.GatherAttributeValue(this, Attribute.Ammo);

    public override int GetBaseValue(Attribute attribute)
    {
        return attribute switch
        {
            Attribute.AttackBonus => baseAttackBonus,
            Attribute.Ammo => baseMaxAmmo,
            _ => base.GetBaseValue(attribute)
        };
    }

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

        baseDamageFormula = new DamageFormula { baseDice = 1, dieSize = 8, bonus = 0, damageType = DamageType.Physical };
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

        int dice = baseDamageFormula.baseDice;
        int dieSize = baseDamageFormula.dieSize;
        int bonus = baseDamageFormula.bonus;

        string dmgStr = bonus != 0 
            ? $"{dice}d{dieSize}{bonus:+0;-0}"
            : $"{dice}d{dieSize}";
        stats.Add(VehicleComponentUI.DisplayStat.Simple("Damage", "DMG", dmgStr));

        int modifiedAttackBonus = GetAttackBonus();
        stats.Add(VehicleComponentUI.DisplayStat.WithTooltip("Attack Bonus", "HIT", Attribute.AttackBonus, baseAttackBonus, modifiedAttackBonus));

        if (baseMaxAmmo != -1)
        {
            int modifiedMaxAmmo = GetMaxAmmo();
            stats.Add(VehicleComponentUI.DisplayStat.BarWithTooltip("Ammo", "AMMO", Attribute.Ammo, currentAmmo, baseMaxAmmo, modifiedMaxAmmo));
        }
        
        stats.AddRange(base.GetDisplayStats());
        
        return stats;
    }
}
