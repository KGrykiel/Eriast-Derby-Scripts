using System;

/// <summary>
/// Flags enum — combined roles supported (e.g. Driver | Gunner for solo vehicles).
/// Roles are mostly implicit based on the components assigned to the seat
/// But the enum is useful for routing. 
/// </summary>
[Flags]
public enum RoleType
{
    /// <summary>No role enabled (default for most components)</summary>
    None = 0,

    /// <summary>Driver role - controls vehicle movement (enabled by DriveComponent)</summary>
    Driver = 1 << 0,      // 1

    /// <summary>Navigator role - assists with pathfinding and stage selection (enabled by NavigatorComponent)</summary>
    Navigator = 1 << 1,   // 2

    /// <summary>Gunner role - operates weapons (enabled by WeaponComponent)</summary>
    Gunner = 1 << 2,      // 4

    /// <summary>Technician role - repairs, manages power, enables/disables components (enabled by TechnicianComponent</summary>
    Technician = 1 << 3,  // 8
    
    // === EXTENSION SLOTS (for future custom roles) ===
    // Custom1 = 1 << 4,  // 16 - e.g., Mystic
    // Custom2 = 1 << 5,  // 32 - e.g., Medic
    // Custom3 = 1 << 6,  // 64
    // Custom4 = 1 << 7,  // 128
}

/// <summary>
/// Taken from obsidian notes, most are unimplemented and just for future reference.
/// </summary>
public enum ComponentType
{
    /// <summary>
    /// Power generation components (mandatory - provides Power Capacity and Power Discharge).
    /// Examples: Mini Power Core, Power Core XL, High Discharge Power Core
    /// </summary>
    PowerCore,
    
    /// <summary>
    /// Power-related accessories that enhance or modify power systems.
    /// Examples: Core-overdriver (backburner), Other Vehicle Recharger
    /// </summary>
    PowerAccessory,
    
    /// <summary>
    /// Vehicle structure/frame (mandatory - provides HP, AC, Component Space).
    /// Examples: Sturdy Chassis, Bullet Chassis, Armored Cockpit, Bridge
    /// </summary>
    Chassis,
    
    /// <summary>
    /// Movement/propulsion systems (enables Driver role).
    /// Examples: Wheels, Levitator Drive, Legs, Creature-drawn
    /// </summary>
    Drive,
    
    /// <summary>
    /// Direct damage-dealing weapons (enables Gunner role per weapon).
    /// Examples: Front Ram, Ship Cannon, Auto-ballista, Flamethrower, Big Fucking Gun
    /// </summary>
    Weapon,
    
    /// <summary>
    /// Utility/support weapons that don't directly deal damage.
    /// Examples: Component-disabler, Flashbang Launcher, Navigator's Radar Jammer
    /// </summary>
    UtilityWeapon,
    
    /// <summary>
    /// Active defensive systems.
    /// Examples: Shieldspawner, Invisi-drive, RGB Flash
    /// </summary>
    ActiveDefense,
    
    /// <summary>
    /// General utility/support components.
    /// Examples: Gyroscope, Farseer Screens, Nitro Booster, Navigator's Telecoms
    /// </summary>
    Utility,
    
    /// <summary>
    /// Scanning, targeting, and information-gathering systems.
    /// Examples: Navigator's Radar, Wall-Hack Visor, Gem of True Sight window
    /// </summary>
    Sensors,
    
    /// <summary>
    /// Communication systems between crew members.
    /// Examples: Communication pipes, Seeing stones, Navigator's Telecoms
    /// </summary>
    Communications,
    
    /// <summary>
    /// Storage and cargo components.
    /// Examples: Dirt bike storage unit, Bay doors
    /// </summary>
    Storage,
    
    /// <summary>
    /// Entertainment or morale-boosting components.
    /// Examples: Magic Honker, Confetti Cannons, Sad Trombone, Sprinklers
    /// </summary>
    Entertainment,
    
    /// <summary>
    /// Special or magical components that don't fit other categories.
    /// Examples: Ted Empowering crystal, Spiritual Weapon Summoner
    /// </summary>
    Special,
    
    /// <summary>
    /// Custom component type - for user-defined components.
    /// </summary>
    Custom
}

public enum ComponentExposure
{
    /// <summary>
    /// Fully exposed, easy to target (weapons, sensors, external armor).
    /// Can be targeted without restrictions.
    /// </summary>
    External,
    
    /// <summary>
    /// Protected by armor or shielding components.
    /// Can only be targeted if shielding component is destroyed or using penetrating attacks (TODO).
    /// </summary>
    Protected,
    
    /// <summary>
    /// Deep inside the vehicle (power core, critical systems).
    /// Requires chassis damage (below e.g. 50% HP) or special abilities to access.
    /// </summary>
    Internal,
    
    /// <summary>
    /// Actively shielded by specific defensive components.
    /// Cannot be targeted while shield component is active.
    /// </summary>
    Shielded
}

public enum ControlType
{
    Player,
    AI
}

public enum VehicleStatus
{
    Active,
    Destroyed,
}
