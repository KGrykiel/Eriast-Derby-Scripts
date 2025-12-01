using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Flexible stat modifier system for vehicle components.
/// Components can contribute any stat they need using a dictionary-based approach.
/// All values are additive - the vehicle's total stats are the sum of all component contributions.
/// </summary>
[System.Serializable]
public class VehicleStatModifiers
{
    // Dictionary of stat name -> value
    // Allows components to add any stat without pre-defining all possibilities
    private Dictionary<string, float> modifiers = new Dictionary<string, float>();
    
    // Common stat name constants for consistency
    public static class StatNames
    {
        public const string HP = "HP";
        public const string AC = "AC";
        public const string ComponentSpace = "ComponentSpace";
        public const string PowerCapacity = "PowerCapacity";
        public const string PowerDischarge = "PowerDischarge";
        public const string Speed = "Speed";
        public const string Mobility = "Mobility";
        public const string Stability = "Stability";
        public const string Weight = "Weight";
        // Add more as needed
    }
    
    /// <summary>
    /// Set a stat modifier value.
    /// </summary>
    public void SetStat(string statName, float value)
    {
        modifiers[statName] = value;
    }
    
    /// <summary>
    /// Get a stat modifier value. Returns 0 if stat not set.
    /// </summary>
    public float GetStat(string statName)
    {
        return modifiers.TryGetValue(statName, out float value) ? value : 0f;
    }
    
    /// <summary>
    /// Check if a stat modifier is set.
    /// </summary>
    public bool HasStat(string statName)
    {
        return modifiers.ContainsKey(statName);
    }
    
    /// <summary>
    /// Get all stat names that this modifier affects.
    /// </summary>
    public IEnumerable<string> GetAllStatNames()
    {
        return modifiers.Keys;
    }
    
    /// <summary>
    /// Add another modifier's values to this one (for aggregation).
    /// </summary>
    public void Add(VehicleStatModifiers other)
    {
        if (other == null) return;
        
        foreach (var statName in other.GetAllStatNames())
        {
            float currentValue = GetStat(statName);
            float otherValue = other.GetStat(statName);
            SetStat(statName, currentValue + otherValue);
        }
    }
    
    /// <summary>
    /// Returns an empty modifier (no bonuses).
    /// </summary>
    public static VehicleStatModifiers Zero => new VehicleStatModifiers();
    
    // Convenience methods for common stats (optional, for cleaner code)
    public int HP
    {
        get => Mathf.RoundToInt(GetStat(StatNames.HP));
        set => SetStat(StatNames.HP, value);
    }
    
    public int AC
    {
        get => Mathf.RoundToInt(GetStat(StatNames.AC));
        set => SetStat(StatNames.AC, value);
    }
    
    public int ComponentSpace
    {
        get => Mathf.RoundToInt(GetStat(StatNames.ComponentSpace));
        set => SetStat(StatNames.ComponentSpace, value);
    }
    
    public int PowerCapacity
    {
        get => Mathf.RoundToInt(GetStat(StatNames.PowerCapacity));
        set => SetStat(StatNames.PowerCapacity, value);
    }
    
    public int PowerDischarge
    {
        get => Mathf.RoundToInt(GetStat(StatNames.PowerDischarge));
        set => SetStat(StatNames.PowerDischarge, value);
    }
    
    public float Speed
    {
        get => GetStat(StatNames.Speed);
        set => SetStat(StatNames.Speed, value);
    }
    
    public float Mobility
    {
        get => GetStat(StatNames.Mobility);
        set => SetStat(StatNames.Mobility, value);
    }
    
    public float Stability
    {
        get => GetStat(StatNames.Stability);
        set => SetStat(StatNames.Stability, value);
    }
    
    public float Weight
    {
        get => GetStat(StatNames.Weight);
        set => SetStat(StatNames.Weight, value);
    }
}
