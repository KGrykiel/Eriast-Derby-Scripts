using System;

namespace Assets.Scripts.Entities
{
    /// <summary>
    /// Capability flags for status effect targeting validation. Combinable via bitwise OR.
    /// e.g. only flammabe entities can gain the "burning" effect.
    /// WIP - mostly stubs now, I need to think more about this design.
    /// </summary>
    [Flags]
    public enum EntityFeature
    {
        None = 0,

        // Core Capabilities
        HasHealth = 1 << 0,
        HasArmor = 1 << 1,
        HasEnergy = 1 << 2,

        // Material Properties
        IsFlammable = 1 << 3,
        IsElectronic = 1 << 4,
        IsMechanical = 1 << 5,
        IsOrganic = 1 << 6,

        // Entity Type (exclusive categories)
        IsLiving = 1 << 7,
        IsMachine = 1 << 8,

        // Optional: Specific immunities (if needed)
        ImmuneToFire = 1 << 9,
        ImmuneToCold = 1 << 10,
        ImmuneToStun = 1 << 11,
    }
}
