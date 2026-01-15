using System;

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
