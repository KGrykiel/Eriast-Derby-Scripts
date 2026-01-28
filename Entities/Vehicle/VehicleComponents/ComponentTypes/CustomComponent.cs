using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Custom component - a flexible component for user-defined purposes.
/// Can be configured to provide modifiers to other components, enable any role, and use any component type.
/// Use this for components that don't fit the standard categories.
/// 
/// To provide bonuses to other components (like HP to chassis or Speed to drive),
/// use the providedModifiers list (inherited from VehicleComponent).
/// </summary>
public class CustomComponent : VehicleComponent
{
    
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
}
