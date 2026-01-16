using System;
using UnityEngine;

public enum Attribute
{
    // Core stats
    Speed,
    ArmorClass,
    AttackBonus,
    Mobility,      // Saving throw stat AND skill check stat - dodging, evasion, piloting, reaction speed
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
    // Future: Add Perception, Mechanics, etc. when you actually need them
}

public enum VehicleStatus
{
    Active,
    Destroyed,
}