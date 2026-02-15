using System;

public enum ModifierType
{
    Flat,        // +X (additive)
    Multiplier,  // ×X (multiplicative, e.g., 1.5 = 150% = +50%)
}

/// <summary>
/// Category of modifier source, for tracking and display purposes.
/// </summary>
public enum ModifierCategory
{
    /// <summary>
    /// Permanent modifier from equipment/components.
    /// Removed when component disabled or destroyed.
    /// </summary>
    Equipment,
    
    /// <summary>
    /// Temporary modifier from status effects (buffs/debuffs).
    /// </summary>
    StatusEffect,
    
    /// <summary>
    /// Dynamic modifier calculated from other attributes.
    /// Not stored on entity - evaluated on-demand during stat calculation.
    /// e.g. speed to AC (fast vehicles harder to hit)
    /// </summary>
    Dynamic,
    
    /// <summary>
    /// Modifier from nearby entities (auras, leadership bonuses).
    /// Future enhancement - not yet implemented.
    /// </summary>
    Aura,
    
    /// <summary>
    /// One-time bonus from skill use (not from status effect).
    /// Future enhancement - not yet implemented.
    /// </summary>
    Skill,
    
    /// <summary>
    /// Other/unknown source.
    /// Fallback category for edge cases.
    /// </summary>
    Other
}

/// <summary>
/// Flat modifiers are additive, Multiplier modifiers are multiplicative.
/// Application order (D&D standard): base -> all Flat -> all Multiplier.
/// </summary>
[Serializable]
public class AttributeModifier
{
    public Attribute Attribute;
    public ModifierType Type;
    public float Value;
    public UnityEngine.Object Source;
    public ModifierCategory Category;
    public string DisplayNameOverride;

    public string SourceDisplayName => DisplayNameOverride ?? (Source != null ? Source.name : "Unknown");
    public bool IsDispellable => Category == ModifierCategory.StatusEffect || Category == ModifierCategory.Aura;
    public bool IsPermanent => Category == ModifierCategory.Equipment;
    public bool IsTemporary => Category == ModifierCategory.StatusEffect || 
                              Category == ModifierCategory.Aura || 
                              Category == ModifierCategory.Skill;

    public AttributeModifier(
        Attribute attribute,
        ModifierType type,
        float value,
        UnityEngine.Object source = null,
        ModifierCategory category = ModifierCategory.Other,
        string displayNameOverride = null
    )
    {
        Attribute = attribute;
        Type = type;
        Value = value;
        Source = source;
        Category = category;
        DisplayNameOverride = displayNameOverride;
    }
}

