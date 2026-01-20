using Assets.Scripts.Core;
using System.Collections.Generic;
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
        componentSpace = 200;  // Consumes component space
        powerDrawPerTurn = 10;  // Requires power to operate
        
        // Set drive-specific stats (already have defaults in field declarations)
        // maxSpeed = 10f;
        // acceleration = 1f;
        // stability = 5f;
        
        // Drive ENABLES the "Driver" role
        enablesRole = true;
        roleType = RoleType.Driver;
    }
    
    void Awake()
    {
        // Set component type (in case Reset wasn't called)
        componentType = ComponentType.Drive;
        
        // Drive ENABLES the "Driver" role
        enablesRole = true;
        roleType = RoleType.Driver;
    }
    
    
    /// <summary>
    /// Get the stats to display in the UI for this drive component.
    /// Uses StatCalculator for modified values.
    /// </summary>
    public override List<VehicleComponentUI.DisplayStat> GetDisplayStats()
    {
        var stats = new List<VehicleComponentUI.DisplayStat>();
        
        // Get modified values from StatCalculator
        float modifiedSpeed = StatCalculator.GatherAttributeValue(this, Attribute.Speed, maxSpeed);
        float modifiedAccel = StatCalculator.GatherAttributeValue(this, Attribute.Acceleration, acceleration);
        float modifiedStab = StatCalculator.GatherAttributeValue(this, Attribute.Stability, stability);
        
        // All stats support modifiers and tooltips
        stats.Add(VehicleComponentUI.DisplayStat.WithTooltip("Speed", "SPD", Attribute.Speed, maxSpeed, modifiedSpeed));
        stats.Add(VehicleComponentUI.DisplayStat.WithTooltip("Acceleration", "ACCEL", Attribute.Acceleration, acceleration, modifiedAccel));
        stats.Add(VehicleComponentUI.DisplayStat.WithTooltip("Stability", "STAB", Attribute.Stability, stability, modifiedStab));
        
        // Add base class stats (power draw)
        stats.AddRange(base.GetDisplayStats());
        
        return stats;
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
