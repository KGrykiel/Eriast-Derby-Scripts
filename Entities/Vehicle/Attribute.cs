using System;
using UnityEngine;

public enum Attribute
{
    // Core stats
    Speed,
    ArmorClass,
    AttackBonus,
    MaxHealth,
    MaxEnergy,
    EnergyRegen,
    
    // Drive stats
    Acceleration,
    Stability,
    
    // Weapon stats
    DamageDice,
    DamageDieSize,
    DamageBonus,
    Ammo,
    
    // Component stats
    ComponentSpace,
    PowerDraw,
    
    // Resistances
    MagicResistance,
    PhysicalResistance,
    
    // Add more as needed
}

public enum VehicleStatus
{
    Active,
    Destroyed,
}