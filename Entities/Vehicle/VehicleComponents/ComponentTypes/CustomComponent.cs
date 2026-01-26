using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Custom component - a flexible component for user-defined purposes.
/// Can be configured to provide modifiers to other components, enable any role, and use any component type.
/// Use this for components that don't fit the standard categories.
/// 
/// NOTE: To provide bonuses to other components (like HP to chassis or Speed to drive),
/// use the providedModifiers list (inherited from VehicleComponent) instead of the
/// deprecated bonus fields below. Those fields are kept for backward compatibility
/// but should be migrated to providedModifiers.
/// </summary>
public class CustomComponent : VehicleComponent
{
    [Header("Legacy Bonus Stats (DEPRECATED - Use providedModifiers instead)")]
    [Tooltip("DEPRECATED: Use providedModifiers with target Chassis and Attribute.MaxHealth instead")]
    public int hpBonus = 0;
    
    [Tooltip("DEPRECATED: Use providedModifiers with target Chassis and Attribute.ArmorClass instead")]
    public int acBonus = 0;
    
    [Tooltip("DEPRECATED: Use providedModifiers with target Drive and Attribute.Speed instead")]
    public float speedBonus = 0f;
    
    [Tooltip("DEPRECATED: Use providedModifiers with target Chassis and Attribute.ComponentSpace instead")]
    public int componentSpaceBonus = 0;
    
    [Tooltip("DEPRECATED: Use providedModifiers with target PowerCore and Attribute.MaxEnergy instead")]
    public int powerCapacityBonus = 0;
    
    [Tooltip("DEPRECATED: Use providedModifiers with target PowerCore instead")]
    public int powerDischargeBonus = 0;
    
    /// <summary>
    /// Called when component is first added or reset in Editor.
    /// Sets default values that appear immediately in Inspector.
    /// </summary>
    void Reset()
    {
        // Set GameObject name (shows in hierarchy)
        gameObject.name = "Custom Component";
        
        // For custom components, allow user to set type
        componentType = ComponentType.Custom;
        
        // Set reasonable defaults using Entity fields
        baseMaxHealth = 50;
        health = 50;
        baseArmorClass = 15;
        baseComponentSpace = 100;  // Most components consume space
        basePowerDrawPerTurn = 0;
        
        // Custom components don't enable roles by default
        roleType = RoleType.None;
    }
    
    void Awake()
    {
        // For custom components, don't lock the type
        // User can change componentType and roleType in Inspector
        // (via the CustomComponentEditor which overrides [ReadOnly])
    }
    
    /// <summary>
    /// Get the stats to display in the UI for this custom component.
    /// Shows all non-zero bonus stats (legacy) and providedModifiers.
    /// </summary>
    public override List<VehicleComponentUI.DisplayStat> GetDisplayStats()
    {
        var stats = new List<VehicleComponentUI.DisplayStat>();
        
        // Legacy bonus display (for backward compatibility)
        if (hpBonus != 0)
            stats.Add(VehicleComponentUI.DisplayStat.Simple("HP Bonus", "HP+", hpBonus));
        
        if (acBonus != 0)
            stats.Add(VehicleComponentUI.DisplayStat.Simple("AC Bonus", "AC+", acBonus));
        
        if (speedBonus != 0)
            stats.Add(VehicleComponentUI.DisplayStat.Simple("Speed Bonus", "SPD+", speedBonus));
        
        if (componentSpaceBonus != 0)
            stats.Add(VehicleComponentUI.DisplayStat.Simple("Space", "SPACE", componentSpaceBonus));
        
        if (powerCapacityBonus != 0)
            stats.Add(VehicleComponentUI.DisplayStat.Simple("Power Cap", "PWR+", powerCapacityBonus));
        
        if (powerDischargeBonus != 0)
            stats.Add(VehicleComponentUI.DisplayStat.Simple("Discharge", "DISC", powerDischargeBonus));
        
        // Show providedModifiers summary
        foreach (var mod in providedModifiers)
        {
            string sign = mod.value >= 0 ? "+" : "";
            stats.Add(VehicleComponentUI.DisplayStat.Simple($"{mod.attribute} to {mod.targetMode}", "", $"{sign}{mod.value}"));
        }
        
        // Add base class stats (power draw)
        stats.AddRange(base.GetDisplayStats());
        
        return stats;
    }
    
    /// <summary>
    /// Called when custom component is destroyed.
    /// </summary>
    protected override void OnComponentDestroyed()
    {
        base.OnComponentDestroyed();
        
        Debug.LogWarning($"[CustomComponent] {name} ({componentType}) destroyed!");
    }
}
