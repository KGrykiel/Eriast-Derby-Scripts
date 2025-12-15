/// <summary>
/// Damage types for the D&D-style combat system.
/// Used for resistance/vulnerability calculations.
/// </summary>
public enum DamageType
{
    // Physical damage types
    Physical,       // Default, generic physical damage
    Bludgeoning,    // Hammers, rams, collisions (can split from Physical later)
    Piercing,       // Crossbows, spears, ballistas
    Slashing,       // Blades, saw weapons
    
    // Elemental damage types
    Fire,           // Flamethrowers, explosions
    Cold,           // Cryo weapons, ice magic
    Lightning,      // Tesla coils, shock weapons
    Acid,           // Chemical sprayers
    
    // Special damage types
    Force,          // Pure magical/mechanical force
    Psychic,        // Mind attacks (rare)
    Necrotic,       // Life-draining
    Radiant,        // Holy/light damage
}
