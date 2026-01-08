using System;
using UnityEngine;

public enum ModifierType
{
    Flat,      // +X
    Percent,   // +X%
}

[Serializable]
public class AttributeModifier
{
    public Attribute Attribute;
    public ModifierType Type;
    public float Value;
    public int DurationTurns; // -1 for permanent
    public UnityEngine.Object Source; // The object that applied this modifier (Stage, Item, etc.)
    public bool local = false;

    public AttributeModifier(
        Attribute attribute,
        ModifierType type,
        float value,
        int durationTurns = -1,
        UnityEngine.Object source = null,
        bool local = false
    )
    {
        Attribute = attribute;
        Type = type;
        Value = value;
        DurationTurns = durationTurns;
        Source = source;
        this.local = local;
    }
}

