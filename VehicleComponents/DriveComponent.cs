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
    
    /// <summary>
    /// Called when component is first added or reset in Editor.
    /// Sets default values that appear immediately in Inspector.
    /// </summary>
    void Reset()
    {
        // Set GameObject name (shows in hierarchy)
        gameObject.name = "Drive";
        
        // Set component identity
        componentType = ComponentType.Drive;
        
        // Set component base stats using Entity fields
        maxHealth = 60;      // Moderately durable
        health = 60;         // Start at full HP
        armorClass = 16;     // Somewhat exposed
        componentSpaceRequired = -200;  // Consumes component space
        powerDrawPerTurn = 10;  // Requires power to operate
        
        // Set drive-specific stats (already have defaults in field declarations)
        // maxSpeed = 10f;
        // acceleration = 1f;
        // stability = 5f;
        
        // Drive ENABLES the "Driver" role
        enablesRole = true;
        roleName = "Driver";
    }
    
    void Awake()
    {
        // Set component type (in case Reset wasn't called)
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
        Debug.LogWarning($"[Drive] {name} destroyed! Vehicle cannot move - Driver role disabled!");
        
        // The base class already logs that the "Driver" role is no longer available
    }
}
