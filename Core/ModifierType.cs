using System;
using UnityEngine;

public enum ModifierType
{
    Flat,      // +X
    Percent,   // +X%
}

/// <summary>
/// A simple stat change. Duration is tracked by StatusEffect (if applicable), not here.
/// Source can be either a StatusEffect (temporary/indefinite) or a Component (permanent equipment).
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

