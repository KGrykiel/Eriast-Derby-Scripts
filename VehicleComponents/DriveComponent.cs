using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// Drive component - the movement/propulsion system of a vehicle.
/// OPTIONAL: Vehicles without drives might be stationary or creature-drawn.
/// Provides: Speed, Acceleration, and Stability stats.
/// ENABLES ROLE: "Driver" - allows a character to control vehicle movement.
/// </summary>
public class DriveComponent : VehicleComponent
{
    [Header("Drive Stats")]
    [Tooltip("Maximum speed this drive can achieve")]
    public float maxSpeed = 10f;
    
    [Tooltip("Acceleration rate")]
    public float acceleration = 1f;
    
    [Tooltip("Stability - resistance to terrain effects and bumps")]
    public float stability = 5f;
    
    void Awake()
    {
        // Set component type
        componentType = ComponentType.Drive;
        
        // Drive ENABLES the "Driver" role
        enablesRole = true;
        roleName = "Driver";
    }
    
    /// <summary>
    /// Drive provides Speed, Acceleration, and Stability to the vehicle.
    /// </summary>
    public override VehicleStatModifiers GetStatModifiers()
    {
        // If drive is destroyed or disabled, it contributes nothing
        if (isDestroyed || isDisabled)
            return VehicleStatModifiers.Zero;
        
        // Create modifiers using the flexible stat system
        var modifiers = new VehicleStatModifiers();
        modifiers.Speed = maxSpeed;
        modifiers.SetStat("Acceleration", acceleration);
        modifiers.Stability = stability;
        
        return modifiers;
    }
    
    /// <summary>
    /// Called when drive is destroyed.
    /// Vehicle loses ability to move (Driver role becomes unavailable).
    /// </summary>
    protected override void OnComponentDestroyed()
    {
        base.OnComponentDestroyed();
        
        // Drive destruction disables movement
        Debug.LogWarning($"[Drive] {componentName} destroyed! Vehicle cannot move - Driver role disabled!");
        
        // The base class already logs that the "Driver" role is no longer available
    }
}
