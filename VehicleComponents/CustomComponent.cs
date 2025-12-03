using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Custom component - a flexible component for user-defined purposes.
/// Can be configured to provide any stats, enable any role, and use any component type.
/// Use this for components that don't fit the standard categories.
/// </summary>
public class CustomComponent : VehicleComponent
{
    [Header("Custom Stats (Flexible)")]
    [Tooltip("HP contribution to vehicle (0 = no contribution)")]
    public int hpBonus = 0;
    
    [Tooltip("AC contribution to vehicle (0 = no contribution)")]
    public int acBonus = 0;
    
    [Tooltip("Speed contribution to vehicle (0 = no contribution)")]
    public float speedBonus = 0f;
    
    [Tooltip("Component Space contribution (positive = provides, negative = consumes)")]
    public int componentSpaceBonus = 0;
    
    [Tooltip("Power Capacity contribution (0 = no contribution)")]
    public int powerCapacityBonus = 0;
    
    [Tooltip("Power Discharge contribution (0 = no contribution)")]
    public int powerDischargeBonus = 0;
    
    /// <summary>
    /// Called when component is first added or reset in Editor.
    /// Sets default values that appear immediately in Inspector.
    /// </summary>
    void Reset()
    {
        // For custom components, allow user to set type
        componentType = ComponentType.Custom;
        componentName = "Custom Component";
        
        // Set reasonable defaults
        componentHP = 50;
        componentAC = 15;
        componentSpaceRequired = -100;  // Most components consume space
        powerDrawPerTurn = 0;
        
        // Custom components don't enable roles by default
        enablesRole = false;
        roleName = "";
    }
    
    void Awake()
    {
        // For custom components, don't lock the type
        // User can change componentType, enablesRole, and roleName in Inspector
        // (via the CustomComponentEditor which overrides [ReadOnly])
        
        // Initialize current HP
        currentHP = componentHP;
    }
    
    /// <summary>
    /// Custom components can provide any combination of stats.
    /// Configured via Inspector.
    /// </summary>
    public override VehicleStatModifiers GetStatModifiers()
    {
        // If component is destroyed or disabled, it contributes nothing
        if (isDestroyed || isDisabled)
            return VehicleStatModifiers.Zero;
        
        // Build modifiers from custom stats
        var modifiers = new VehicleStatModifiers();
        
        if (hpBonus != 0)
            modifiers.HP = hpBonus;
        
        if (acBonus != 0)
            modifiers.AC = acBonus;
        
        if (speedBonus != 0)
            modifiers.Speed = speedBonus;
        
        if (componentSpaceBonus != 0)
            modifiers.ComponentSpace = componentSpaceBonus;
        
        if (powerCapacityBonus != 0)
            modifiers.PowerCapacity = powerCapacityBonus;
        
        if (powerDischargeBonus != 0)
            modifiers.PowerDischarge = powerDischargeBonus;
        
        return modifiers;
    }
    
    /// <summary>
    /// Called when custom component is destroyed.
    /// </summary>
    protected override void OnComponentDestroyed()
    {
        base.OnComponentDestroyed();
        
        Debug.LogWarning($"[CustomComponent] {componentName} ({componentType}) destroyed!");
    }
}
