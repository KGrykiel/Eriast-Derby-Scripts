using System;
using UnityEngine;

public enum ModifierType
{
    Flat,        // +X (additive)
    Multiplier,  // ×X (multiplicative, e.g., 1.5 = 150% = +50%)
}

/// <summary>
/// A simple stat change. Duration is tracked by StatusEffect (if applicable), not here.
/// Source can be either a StatusEffect (temporary/indefinite) or a Component (permanent equipment).
/// 
/// Modifier Types:
/// - Flat: Additive bonus/penalty (e.g., +10 Speed)
/// - Multiplier: Multiplicative modifier following D&D standard (e.g., 1.5 = +50%, 0.5 = -50%, 2.0 = ×2)
/// 
/// Application Order (D&D standard):
/// 1. Start with base value
/// 2. Apply all Flat modifiers (additive)
/// 3. Apply all Multiplier modifiers (multiplicative)
/// 
/// Example:
///   Base Speed: 50
///   Flat +10: 50 + 10 = 60
///   Multiplier 1.5×: 60 × 1.5 = 90
/// </summary>
[Serializable]
public class AttributeModifier
{
    // What it modifies
    public Attribute Attribute;
    public ModifierType Type;
    public float Value;
    
    // Source tracking (StatusEffect OR Component)
    public UnityEngine.Object Source;
    
    /// <summary>
    /// Display name for UI (shows source name or "Unknown")
    /// </summary>
    public string SourceDisplayName => Source?.name ?? "Unknown";

    public AttributeModifier(
        Attribute attribute,
        ModifierType type,
        float value,
        UnityEngine.Object source = null
    )
    {
        Attribute = attribute;
        Type = type;
        Value = value;
        Source = source;
    }
}

