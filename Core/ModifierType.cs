using System;

public enum ModifierType
{
    Flat,        // +X (additive)
    Multiplier,  // ×X (multiplicative, e.g., 1.5 = 150% = +50%)
}

/// <summary>
/// Category of a modifier, indicating its source type and behavior.
/// Used for grouping in tooltips, filtering for dispel, and semantic clarity.
/// </summary>
public enum ModifierCategory
{
    /// <summary>
    /// Permanent modifier from equipment/components.
    /// Not dispellable, persists as long as component is installed.
    /// </summary>
    Equipment,
    
    /// <summary>
    /// Temporary modifier from status effects (buffs/debuffs).
    /// Dispellable, has duration, tracked by AppliedStatusEffect.
    /// </summary>
    StatusEffect,
    
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
    
    // Category tracking (for grouping and filtering)
    public ModifierCategory Category;
    
    /// <summary>
    /// Display name for UI (shows source name or "Unknown")
    /// </summary>
    public string SourceDisplayName => Source != null ? Source.name : null ?? "Unknown";
    
    /// <summary>
    /// Is this modifier dispellable? (Status effects are, equipment is not)
    /// </summary>
    public bool IsDispellable => Category == ModifierCategory.StatusEffect || Category == ModifierCategory.Aura;
    
    /// <summary>
    /// Is this modifier permanent? (Equipment is, status effects are not)
    /// </summary>
    public bool IsPermanent => Category == ModifierCategory.Equipment;
    
    /// <summary>
    /// Is this modifier from a temporary effect?
    /// </summary>
    public bool IsTemporary => Category == ModifierCategory.StatusEffect || 
                              Category == ModifierCategory.Aura || 
                              Category == ModifierCategory.Skill;

    public AttributeModifier(
        Attribute attribute,
        ModifierType type,
        float value,
        UnityEngine.Object source = null,
        ModifierCategory category = ModifierCategory.Other
    )
    {
        Attribute = attribute;
        Type = type;
        Value = value;
        Source = source;
        Category = category;
    }
}

