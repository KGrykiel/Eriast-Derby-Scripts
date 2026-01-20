// ==================== VEHICLE ENUMS ====================
// All vehicle-specific enums consolidated in one place.
// System-wide enums (e.g., Attribute) are in separate files.

/// <summary>
/// Defines the types of roles that vehicle components can enable.
/// Each role allows a character to perform specific actions during combat.
/// </summary>
public enum RoleType
{
    /// <summary>No role enabled (default for most components)</summary>
    None = 0,

    /// <summary>Driver role - controls vehicle movement (enabled by DriveComponent)</summary>
    Driver,

    /// <summary>Navigator role - assists with pathfinding and stage selection</summary>
    Navigator,

    /// <summary>Gunner role - operates weapons (enabled by WeaponComponent)</summary>
    Gunner,

    /// <summary>Technician role - repairs, manages power, enables/disables components</summary>
    Technician
}

/// <summary>
/// Defines the type/category of a vehicle component.
/// Used for organization, filtering, and UI display.
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

/// <summary>
/// Defines how exposed/accessible a component is for targeting in combat.
/// Determines whether a component can be directly targeted or requires special conditions.
/// </summary>
public enum ComponentExposure
{
    /// <summary>
    /// Fully exposed, easy to target (weapons, sensors, external armor).
    /// Can be targeted without restrictions.
    /// </summary>
    External,
    
    /// <summary>
    /// Protected by armor or shielding components.
    /// Can only be targeted if shielding component is destroyed or using penetrating attacks.
    /// </summary>
    Protected,
    
    /// <summary>
    /// Deep inside the vehicle (power core, critical systems).
    /// Requires chassis damage (below 50% HP) or special abilities to access.
    /// </summary>
    Internal,
    
    /// <summary>
    /// Actively shielded by specific defensive components.
    /// Cannot be targeted while shield component is active.
    /// </summary>
    Shielded
}

/// <summary>
/// Defines who controls the vehicle.
/// </summary>
public enum ControlType
{
    Player,
    AI
}

/// <summary>
/// Defines the operational status of a vehicle.
/// </summary>
public enum VehicleStatus
{
    Active,
    Destroyed,
}
