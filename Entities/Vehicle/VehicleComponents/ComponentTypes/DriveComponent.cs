using Assets.Scripts.Core;
using Assets.Scripts.Logging;
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
    
    [Tooltip("Acceleration rate (how fast speed changes per turn)")]
    public float acceleration = 1f;
    
    [Tooltip("Stability - resistance to terrain effects and bumps")]
    public float stability = 5f;
    
    [Header("Speed Management")]
    [Tooltip("Current actual speed (can be below maxSpeed)")]
    [ReadOnly]
    public float currentSpeed = 0f;
    
    [Header("Mechanical Properties")]
    [Tooltip("Mechanical friction from drive system (rolling resistance, bearings, gears)")]
    public float baseFriction = 1.0f;
    
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
        roleType = RoleType.Driver;
    }
    
    void Awake()
    {
        // Set component type (in case Reset wasn't called)
        componentType = ComponentType.Drive;
        
        // Drive ENABLES the "Driver" role
        roleType = RoleType.Driver;
        
        // Initialize speed to 0 (vehicle starts stationary)
        currentSpeed = 0f;
    }
    
    // ==================== POWER MANAGEMENT ====================
    
    /// <summary>
    /// Get actual power draw based on current speed.
    /// Uses physics-based friction model: Power = basePowerDraw + baseFriction + (chassis.dragCoefficient * currentSpeed)
    /// baseFriction = mechanical friction from drive system
    /// dragCoefficient = aerodynamic drag from chassis/vehicle body
    /// Higher speeds = more power consumption due to air resistance.
    /// </summary>
    public override int GetActualPowerDraw()
    {
        if (!isPowered || isDestroyed) return 0;
        
        // Get base friction with modifiers (mechanical from drive)
        float modifiedBaseFriction = StatCalculator.GatherAttributeValue(
            this,
            Attribute.BaseFriction,
            baseFriction
        );
        
        // Get chassis drag coefficient with modifiers (aerodynamics of vehicle body)
        float vehicleDrag = 0.1f; // Default fallback
        if (parentVehicle?.chassis != null)
        {
            vehicleDrag = StatCalculator.GatherAttributeValue(
                parentVehicle.chassis,
                Attribute.DragCoefficient,
                parentVehicle.chassis.dragCoefficient
            );
        }
        
        // Physics: Power needed to overcome friction
        // modifiedBaseFriction = mechanical (drive-specific, affected by terrain/maintenance)
        // vehicleDrag * speed = aerodynamic (chassis-specific, modified by components)
        float frictionForce = modifiedBaseFriction + (vehicleDrag * currentSpeed);
        
        // Get base power with modifiers
        float basePower = StatCalculator.GatherAttributeValue(
            this, 
            Attribute.PowerDraw, 
            powerDrawPerTurn  // Base engine idle power
        );
        
        int totalCost = Mathf.RoundToInt(basePower + frictionForce);
        return Mathf.Max(0, totalCost);
    }
    
    /// <summary>
    /// Apply natural friction when drive is unpowered (destroyed/disabled).
    /// Called at start of turn BEFORE power draw.
    /// friction_loss = baseFriction + (chassis.dragCoefficient * currentSpeed)
    /// Vehicle gradually decelerates to a stop.
    /// </summary>
    public void ApplyFriction()
    {
        if (currentSpeed <= 0) return;
        
        // Get base friction with modifiers (mechanical from drive)
        float modifiedBaseFriction = StatCalculator.GatherAttributeValue(
            this,
            Attribute.BaseFriction,
            baseFriction
        );
        
        // Get chassis drag coefficient with modifiers (aerodynamics of vehicle body)
        float vehicleDrag = 0.1f; // Default fallback
        if (parentVehicle?.chassis != null)
        {
            vehicleDrag = StatCalculator.GatherAttributeValue(
                parentVehicle.chassis,
                Attribute.DragCoefficient,
                parentVehicle.chassis.dragCoefficient
            );
        }
        
        // Physics: friction = constant mechanical + aerodynamic drag
        float frictionLoss = modifiedBaseFriction + (vehicleDrag * currentSpeed);
        
        float oldSpeed = currentSpeed;
        currentSpeed = Mathf.Max(0, currentSpeed - frictionLoss);
        
        if (parentVehicle != null && Mathf.Abs(oldSpeed - currentSpeed) > 0.01f)
        {
            RaceHistory.Log(
                Assets.Scripts.Logging.EventType.Movement,
                EventImportance.Low,
                $"{parentVehicle.vehicleName} slowed by friction: {oldSpeed:F1} → {currentSpeed:F1}",
                parentVehicle.currentStage,
                parentVehicle
            ).WithMetadata("oldSpeed", oldSpeed)
             .WithMetadata("newSpeed", currentSpeed)
             .WithMetadata("frictionLoss", frictionLoss)
             .WithMetadata("reason", "UnpoweredFriction");
        }
    }
    
    // ==================== ACCELERATION SYSTEM ====================
    
    /// <summary>
    /// Accelerate the vehicle. Called by Driver during turn.
    /// Speed changes affect NEXT turn's power cost.
    /// </summary>
    public void Accelerate(float amount)
    {
        if (isDestroyed) return;
        
        // Can't accelerate if unpowered (but can coast/decelerate)
        if (!isPowered && amount > 0) return;
        
        float modifiedAccel = StatCalculator.GatherAttributeValue(
            this, 
            Attribute.Acceleration, 
            acceleration
        );
        
        float modifiedMaxSpeed = StatCalculator.GatherAttributeValue(
            this, 
            Attribute.Speed, 
            maxSpeed
        );
        
        float oldSpeed = currentSpeed;
        currentSpeed = Mathf.Clamp(
            currentSpeed + (amount * modifiedAccel), 
            0f, 
            modifiedMaxSpeed
        );
        
        if (Mathf.Abs(currentSpeed - oldSpeed) > 0.01f && parentVehicle != null)
        {
            RaceHistory.Log(
                Assets.Scripts.Logging.EventType.Movement,
                EventImportance.Low,
                $"{parentVehicle.vehicleName} speed changed: {oldSpeed:F1} → {currentSpeed:F1}",
                parentVehicle.currentStage,
                parentVehicle
            ).WithMetadata("oldSpeed", oldSpeed)
             .WithMetadata("newSpeed", currentSpeed)
             .WithMetadata("maxSpeed", modifiedMaxSpeed);
        }
    }
    
    /// <summary>
    /// Decelerate the vehicle (braking, friction, etc.)
    /// </summary>
    public void Decelerate(float amount)
    {
        Accelerate(-amount);  // Negative acceleration
    }
    
    /// <summary>
    /// Set speed to maximum (full throttle)
    /// </summary>
    public void FullThrottle()
    {
        Accelerate(maxSpeed);  // Accelerate as much as possible (clamped by acceleration rate)
    }
    
    /// <summary>
    /// Emergency brake (rapid deceleration)
    /// </summary>
    public void Brake()
    {
        Decelerate(maxSpeed);  // Decelerate as much as possible (clamped by deceleration rate)
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
        float modifiedFriction = StatCalculator.GatherAttributeValue(this, Attribute.BaseFriction, baseFriction);
        
        // Core drive stats
        stats.Add(VehicleComponentUI.DisplayStat.WithTooltip("Max Speed", "MSPD", Attribute.Speed, maxSpeed, modifiedSpeed));
        stats.Add(VehicleComponentUI.DisplayStat.WithTooltip("Acceleration", "ACCEL", Attribute.Acceleration, acceleration, modifiedAccel));
        stats.Add(VehicleComponentUI.DisplayStat.WithTooltip("Stability", "STAB", Attribute.Stability, stability, modifiedStab));
        
        // Current runtime speed (always show - players should always know their current speed)
        stats.Add(VehicleComponentUI.DisplayStat.Simple("Current Speed", "CUR", currentSpeed));
        
        // Physics properties
        stats.Add(VehicleComponentUI.DisplayStat.WithTooltip("Base Friction", "FRIC", Attribute.BaseFriction, baseFriction, modifiedFriction));
        
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
