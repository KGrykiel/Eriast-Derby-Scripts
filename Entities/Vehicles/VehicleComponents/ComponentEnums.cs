using System;
using SerializeReferenceEditor;
using UnityEngine;

namespace Assets.Scripts.Entities.Vehicles.VehicleComponents
{
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
    /// Defines how exposed a component is for targeting.
    /// Each concrete type carries only the data relevant to that exposure mode.
    /// </summary>
    public interface IExposureConfig { }

    /// <summary>Fully exposed — can be targeted without restrictions.</summary>
    [Serializable]
    [SRName("External")]
    public class ExternalExposure : IExposureConfig { }

    /// <summary>
    /// Protected by armor or shielding components.
    /// Inaccessible until the shielding component is destroyed or using penetrating attacks (TODO).
    /// </summary>
    [Serializable]
    [SRName("Protected")]
    public class ProtectedExposure : IExposureConfig
    {
        [Tooltip("Component that shields this one (drag component reference here)")]
        public VehicleComponent shieldedBy;
    }

    /// <summary>
    /// Deep inside the vehicle (power core, critical systems).
    /// Requires sufficient chassis damage to access.
    /// </summary>
    [Serializable]
    [SRName("Internal")]
    public class InternalExposure : IExposureConfig
    {
        [Tooltip("Required chassis damage % to access (e.g., 50 = 50% damage)")]
        [Range(0, 100)]
        public int accessThreshold = 50;
    }

    /// <summary>
    /// Actively shielded by a specific defensive component.
    /// Cannot be targeted while the shield component is active.
    /// </summary>
    [Serializable]
    [SRName("Shielded")]
    public class ShieldedExposure : IExposureConfig
    {
        [Tooltip("Component that actively shields this one (drag component reference here)")]
        public VehicleComponent shieldedBy;
    }
}