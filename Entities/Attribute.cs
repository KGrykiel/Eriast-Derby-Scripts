public enum Attribute
{
    // Core stats
    MaxSpeed,
    ArmorClass,
    AttackBonus,
    Mobility,      // Saving throw stat AND skill check stat - dodging, evasion, piloting, reaction speed
    MaxHealth,
    MaxEnergy,
    EnergyRegen,
    
    // Drive stats
    Acceleration,
    Stability,
    
    // Physics stats
    BaseFriction,     // Mechanical friction (on drive, modified by terrain/status effects)
    DragCoefficient,  // Aerodynamic drag (on chassis, modified by components)
    
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