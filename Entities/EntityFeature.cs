using System;

namespace Entities
{
    /// <summary>
    /// Feature flags for entity capabilities and properties.
    /// Used for capability-based targeting validation for status effects.
    /// Multiple flags can be combined using bitwise OR operations.
    /// 
    /// Example usage:
    ///   EntityFeature chassisFeatures = EntityFeature.HasHealth | EntityFeature.HasArmor | EntityFeature.IsMechanical;
    ///   bool canBurn = (entity.features &amp; EntityFeature.CanBurn) != 0;
    /// </summary>
    [Flags]
    public enum EntityFeature
    {
        None = 0,

        // Core Capabilities
        HasHealth = 1 << 0,        // Entity has HP that can be damaged
        HasArmor = 1 << 1,         // Entity has AC for defense
        HasEnergy = 1 << 2,        // Entity has energy/mana/power

        // Material Properties (determines vulnerability)
        IsFlammable = 1 << 3,      // Can catch fire (wood, fuel, fabric, some metals)
        IsElectronic = 1 << 4,     // Contains electronics (vulnerable to EMP)
        IsMechanical = 1 << 5,     // Mechanical parts (vulnerable to rust, corrosion)
        IsOrganic = 1 << 6,        // Organic material (vulnerable to poison, disease)

        // Entity Type (exclusive categories)
        IsLiving = 1 << 7,         // Living creature (can be stunned, frightened, etc.)
        IsMachine = 1 << 8,        // Machine/construct (immune to organic effects)

        // Optional: Specific immunities (if needed)
        ImmuneToFire = 1 << 9,     // Cannot burn (even if flammable material)
        ImmuneToCold = 1 << 10,    // Cannot be frozen
        ImmuneToStun = 1 << 11,    // Cannot be stunned (mindless constructs)
    }
}
