using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// A single modifier contributing to a roll.
/// Tracks the source of each bonus/penalty for transparent logging.
/// </summary>
[System.Serializable]
public struct RollModifier
{
    public string name;        // "Weapon Attack Bonus", "Component Targeting Penalty"
    public int value;          // +2 or -3
    public string source;      // "Heavy Crossbow", "Power Shot"
    
    public RollModifier(string name, int value, string source = null)
    {
        this.name = name;
        this.value = value;
        this.source = source ?? name;
    }
    
    public override string ToString()
    {
        string sign = value >= 0 ? "+" : "";
        return $"{name}: {sign}{value}";
    }
}

/// <summary>
/// Category of roll for display purposes.
/// </summary>
public enum RollCategory
{
    Attack,         // d20 vs AC
    SkillCheck,     // d20 vs DC
    SavingThrow,    // d20 vs DC (future)
    Damage,         // XdY damage
    Healing,        // XdY healing (future)
    Other           // Misc rolls
}

/// <summary>
/// Complete breakdown of a d20 roll showing base roll and all modifiers.
/// Used for transparent logging and UI tooltips.
/// </summary>
[System.Serializable]
public class RollBreakdown
{
    public RollCategory category;
    public int baseRoll;                   // The actual die result (e.g., 15 on d20)
    public int dieSize;                    // Size of die rolled (20 for d20)
    public int diceCount;                  // Number of dice (usually 1 for d20)
    public List<RollModifier> modifiers;  // All bonuses and penalties
    public int targetValue;                // AC, DC, etc. (0 if not applicable)
    public string targetName;              // "AC", "DC", "Magic Resistance"
    public bool? success;                  // null if not yet evaluated, true/false after
    
    public RollBreakdown()
    {
        modifiers = new List<RollModifier>();
        dieSize = 20;
        diceCount = 1;
    }
    
    /// <summary>
    /// Create a d20 roll breakdown.
    /// </summary>
    public static RollBreakdown D20(int baseRoll, RollCategory category = RollCategory.Attack)
    {
        return new RollBreakdown
        {
            baseRoll = baseRoll,
            dieSize = 20,
            diceCount = 1,
            category = category,
            modifiers = new List<RollModifier>()
        };
    }
    
    /// <summary>
    /// Total after all modifiers.
    /// </summary>
    public int Total
    {
        get
        {
            int total = baseRoll;
            foreach (var mod in modifiers)
                total += mod.value;
            return total;
        }
    }
    
    /// <summary>
    /// Sum of all modifiers (excluding base roll).
    /// </summary>
    public int TotalModifier
    {
        get
        {
            int total = 0;
            foreach (var mod in modifiers)
                total += mod.value;
            return total;
        }
    }
    
    /// <summary>
    /// Add a modifier to this roll. Fluent API.
    /// </summary>
    public RollBreakdown WithModifier(string name, int value, string source = null)
    {
        if (value != 0) // Only add non-zero modifiers
        {
            modifiers.Add(new RollModifier(name, value, source));
        }
        return this;
    }
    
    /// <summary>
    /// Set the target value (AC, DC) and determine success. Fluent API.
    /// </summary>
    public RollBreakdown Against(int target, string targetName = "AC")
    {
        this.targetValue = target;
        this.targetName = targetName;
        this.success = Total >= target;
        return this;
    }
    
    /// <summary>
    /// Short summary: "15 (d20: 12, +3 modifiers)" or "15 vs AC 14 - HIT"
    /// </summary>
    public string ToShortString()
    {
        string modStr = TotalModifier >= 0 ? $"+{TotalModifier}" : $"{TotalModifier}";
        string result = $"{Total} (d{dieSize}: {baseRoll}{modStr})";
        
        if (targetValue > 0 && success.HasValue)
        {
            result += $" vs {targetName} {targetValue}";
            result += success.Value ? " - HIT" : " - MISS";
        }
        
        return result;
    }
    
    /// <summary>
    /// Full breakdown for tooltips/hover display.
    /// </summary>
    public string ToDetailedString()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Roll Breakdown ({category}):");
        sb.AppendLine($"  Base {diceCount}d{dieSize}: {baseRoll}");
        
        foreach (var mod in modifiers)
        {
            string sign = mod.value >= 0 ? "+" : "";
            string sourceInfo = mod.source != mod.name ? $" ({mod.source})" : "";
            sb.AppendLine($"  {mod.name}: {sign}{mod.value}{sourceInfo}");
        }
        
        sb.AppendLine($"  ─────────────");
        sb.AppendLine($"  Total: {Total}");
        
        if (targetValue > 0)
        {
            sb.AppendLine($"  vs {targetName}: {targetValue}");
            if (success.HasValue)
            {
                sb.AppendLine($"  Result: {(success.Value ? "SUCCESS" : "FAILURE")}");
            }
        }
        
        return sb.ToString();
    }
    
    /// <summary>
    /// Convert to dictionary for metadata storage.
    /// </summary>
    public Dictionary<string, object> ToMetadata()
    {
        var metadata = new Dictionary<string, object>
        {
            { "rollCategory", category.ToString() },
            { "baseRoll", baseRoll },
            { "totalModifier", TotalModifier },
            { "total", Total },
            { "targetValue", targetValue },
            { "success", success }
        };
        
        // Add individual modifiers
        for (int i = 0; i < modifiers.Count; i++)
        {
            metadata[$"modifier_{i}_name"] = modifiers[i].name;
            metadata[$"modifier_{i}_value"] = modifiers[i].value;
            metadata[$"modifier_{i}_source"] = modifiers[i].source;
        }
        metadata["modifierCount"] = modifiers.Count;
        
        return metadata;
    }
}

/// <summary>
/// A single component of damage (weapon dice, skill dice, etc.)
/// </summary>
[System.Serializable]
public struct DamageComponent
{
    public string name;      // "Weapon", "Skill Bonus", "Sneak Attack"
    public int diceCount;    // Number of dice
    public int dieSize;      // Size of dice (d6, d8, etc.)
    public int bonus;        // Flat bonus
    public int rolled;       // Sum of dice (without bonus)
    public int total;        // rolled + bonus
    public string source;    // "Heavy Crossbow", "Power Shot"
    
    public DamageComponent(string name, int diceCount, int dieSize, int bonus, int rolled, string source = null)
    {
        this.name = name;
        this.diceCount = diceCount;
        this.dieSize = dieSize;
        this.bonus = bonus;
        this.rolled = rolled;
        this.total = rolled + bonus;
        this.source = source ?? name;
    }
    
    public string ToDiceString()
    {
        string result = $"{diceCount}d{dieSize}";
        if (bonus > 0) result += $"+{bonus}";
        else if (bonus < 0) result += $"{bonus}";
        return result;
    }
    
    public override string ToString()
    {
        return $"{name}: {ToDiceString()} = {total}";
    }
}

/// <summary>
/// Complete breakdown of a damage calculation showing all dice and modifiers.
/// Used for transparent logging and UI tooltips.
/// </summary>
[System.Serializable]
public class DamageBreakdown
{
    public DamageType damageType;
    public List<DamageComponent> components;  // Weapon dice, skill dice, etc.
    public int rawTotal;                      // Before resistances
    public ResistanceLevel resistanceLevel;
    public int finalDamage;                   // After resistances
    
    public DamageBreakdown()
    {
        components = new List<DamageComponent>();
        resistanceLevel = ResistanceLevel.Normal;
    }
    
    /// <summary>
    /// Create a new damage breakdown.
    /// </summary>
    public static DamageBreakdown Create(DamageType type)
    {
        return new DamageBreakdown
        {
            damageType = type,
            components = new List<DamageComponent>()
        };
    }
    
    /// <summary>
    /// Add a damage component (e.g., weapon dice, skill dice). Fluent API.
    /// </summary>
    public DamageBreakdown AddComponent(string name, int diceCount, int dieSize, int bonus, int rolled, string source = null)
    {
        components.Add(new DamageComponent(name, diceCount, dieSize, bonus, rolled, source));
        RecalculateTotal();
        return this;
    }
    
    /// <summary>
    /// Add a flat damage modifier (e.g., strength bonus). Fluent API.
    /// </summary>
    public DamageBreakdown AddFlat(string name, int value, string source = null)
    {
        if (value != 0)
        {
            components.Add(new DamageComponent(name, 0, 0, value, 0, source));
            RecalculateTotal();
        }
        return this;
    }
    
    /// <summary>
    /// Apply resistance and calculate final damage. Fluent API.
    /// </summary>
    public DamageBreakdown WithResistance(ResistanceLevel level)
    {
        resistanceLevel = level;
        
        switch (level)
        {
            case ResistanceLevel.Vulnerable:
                finalDamage = rawTotal * 2;
                break;
            case ResistanceLevel.Resistant:
                finalDamage = rawTotal / 2;
                break;
            case ResistanceLevel.Immune:
                finalDamage = 0;
                break;
            case ResistanceLevel.Normal:
            default:
                finalDamage = rawTotal;
                break;
        }
        
        return this;
    }
    
    private void RecalculateTotal()
    {
        rawTotal = 0;
        foreach (var comp in components)
            rawTotal += comp.total;
        
        // Recalculate final with current resistance
        WithResistance(resistanceLevel);
    }
    
    /// <summary>
    /// Short summary: "15 Fire" or "15 Fire (Resistant)"
    /// </summary>
    public string ToShortString()
    {
        string resistStr = resistanceLevel != ResistanceLevel.Normal 
            ? $" ({resistanceLevel})" 
            : "";
        return $"{finalDamage} {damageType}{resistStr}";
    }
    
    /// <summary>
    /// Full breakdown for tooltips/hover display.
    /// </summary>
    public string ToDetailedString()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Damage Breakdown ({damageType}):");
        
        foreach (var comp in components)
        {
            if (comp.diceCount > 0)
            {
                string diceStr = comp.ToDiceString();
                string resistMod = resistanceLevel != ResistanceLevel.Normal ? GetResistanceMultiplier() : "";
                sb.AppendLine($"  {comp.name}: ({diceStr}){resistMod} = {comp.total} ({comp.source})");
            }
            else if (comp.bonus != 0)
            {
                string sign = comp.bonus >= 0 ? "+" : "";
                sb.AppendLine($"  {comp.name}: {sign}{comp.bonus} ({comp.source})");
            }
        }
        
        sb.AppendLine($"  ─────────────");
        sb.AppendLine($"  Subtotal: {rawTotal}");
        
        if (resistanceLevel != ResistanceLevel.Normal)
        {
            string multiplier = GetResistanceMultiplier();
            sb.AppendLine($"  {resistanceLevel}: {multiplier}");
        }
        
        sb.AppendLine($"  Final: {finalDamage} {damageType}");
        
        return sb.ToString();
    }
    
    /// <summary>
    /// Get resistance multiplier string for display.
    /// </summary>
    private string GetResistanceMultiplier()
    {
        return resistanceLevel switch
        {
            ResistanceLevel.Vulnerable => " (×2)",
            ResistanceLevel.Resistant => " (×0.5)",
            ResistanceLevel.Immune => " (×0)",
            _ => ""
        };
    }
    
    /// <summary>
    /// Convert to dictionary for metadata storage.
    /// </summary>
    public Dictionary<string, object> ToMetadata()
    {
        var metadata = new Dictionary<string, object>
        {
            { "damageType", damageType.ToString() },
            { "rawTotal", rawTotal },
            { "resistanceLevel", resistanceLevel.ToString() },
            { "finalDamage", finalDamage },
            { "componentCount", components.Count }
        };
        
        // Add individual components
        for (int i = 0; i < components.Count; i++)
        {
            metadata[$"component_{i}_name"] = components[i].name;
            metadata[$"component_{i}_dice"] = components[i].ToDiceString();
            metadata[$"component_{i}_total"] = components[i].total;
            metadata[$"component_{i}_source"] = components[i].source;
        }
        
        return metadata;
    }
}
