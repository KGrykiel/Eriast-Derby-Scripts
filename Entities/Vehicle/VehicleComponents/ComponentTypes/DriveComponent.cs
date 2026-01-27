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
    [SerializeField]
    [Tooltip("Maximum speed this drive can achieve (base value before modifiers)")]
    private float baseMaxSpeed = 10f;
    
    [SerializeField]
    [Tooltip("Maximum speed increase per turn (base value before modifiers)")]
    private float baseAcceleration = 1f;
    
    [SerializeField]
    [Tooltip("Maximum speed decrease per turn when braking (base value before modifiers)")]
    private float baseDeceleration = 2f;
    
    [SerializeField]
    [Tooltip("Stability - resistance to terrain effects and bumps (base value before modifiers)")]
    private float baseStability = 5f;
    
    [Header("Speed Management")]
    [Tooltip("Current actual speed in units/turn")]
    [ReadOnly]
    private float currentSpeed = 0f;

    [SerializeField]
    [Tooltip("Target speed as proportion of maxSpeed (0.0 = stopped, 1.0 = full speed). Set by Driver during action phase.")]
    [Range(0f, 1.0f)]
    [ReadOnly]
    private float targetSpeed = 0f;
    
    // Cached maxSpeed to detect changes from buffs/debuffs
    private float lastKnownMaxSpeed;
    
    [Header("Mechanical Properties")]
    [SerializeField]
    [Tooltip("Mechanical friction from drive system (rolling resistance, bearings, gears) (base value before modifiers)")]
    private float friction = 1.0f;
    
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
        baseMaxHealth = 60;      // Moderately durable
        health = 60;         // Start at full HP
        baseArmorClass = 16;     // Somewhat exposed
        baseComponentSpace = 200;  // Consumes component space
        basePowerDrawPerTurn = 10;  // Requires power to operate
        
        // Set drive-specific stats (already have defaults in field declarations)
        // baseMaxSpeed = 10f;
        // baseAcceleration = 1f;
        // baseDeceleration = 2f;
        // baseStability = 5f;
        
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
        lastKnownMaxSpeed = baseMaxSpeed;
    }
    
    // ==================== STAT ACCESSORS ====================
    // These methods provide the single source of truth for stat values with modifiers.
    // UI and game logic should use these instead of accessing fields directly.
    
    // Base value accessors (return raw field values without modifiers)
    public float GetBaseMaxSpeed() => baseMaxSpeed;
    public float GetBaseAcceleration() => baseAcceleration;
    public float GetBaseDeceleration() => baseDeceleration;
    public float GetBaseStability() => baseStability;
    public float GetBaseFriction() => friction;
    
    // Modified value accessors (return values with all modifiers applied via StatCalculator)
    public float GetMaxSpeed() => StatCalculator.GatherAttributeValue(this, Attribute.MaxSpeed, baseMaxSpeed);
    public float GetAcceleration() => StatCalculator.GatherAttributeValue(this, Attribute.Acceleration, baseAcceleration);
    public float GetDeceleration() => StatCalculator.GatherAttributeValue(this, Attribute.Deceleration, baseDeceleration);
    public float GetStability() => StatCalculator.GatherAttributeValue(this, Attribute.Stability, baseStability);
    public float GetFriction() => StatCalculator.GatherAttributeValue(this, Attribute.BaseFriction, friction);
    
    // Runtime state accessor (not a stat, just current value)
    public float GetCurrentSpeed() => currentSpeed;
    
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
        
        // Get friction with modifiers (mechanical from drive)
        float modifiedFriction = GetFriction();
        
        // Get chassis drag coefficient with modifiers (aerodynamics of vehicle body)
        float vehicleDrag = 0.1f; // Default fallback
        if (parentVehicle != null ? parentVehicle.chassis : null != null)
        {
            vehicleDrag = parentVehicle.chassis.GetDragCoefficient();
        }
        
        // Physics: Power needed to overcome friction
        // modifiedFriction = mechanical (drive-specific, affected by terrain/maintenance)
        // vehicleDrag * speed = aerodynamic (chassis-specific, modified by components)
        float frictionForce = modifiedFriction + (vehicleDrag * currentSpeed);
        
        // Get base power with modifiers using accessor method
        float basePower = GetPowerDrawPerTurn();
        
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
        
        // Get friction with modifiers (mechanical from drive)
        float modifiedFriction = GetFriction();
        
        // Get chassis drag coefficient with modifiers (aerodynamics of vehicle body)
        float vehicleDrag = 0.1f; // Default fallback
        if (parentVehicle?.chassis != null)
        {
            vehicleDrag = parentVehicle.chassis.GetDragCoefficient();
        }
        
        // Physics: friction = constant mechanical + aerodynamic drag
        float frictionLoss = modifiedFriction + (vehicleDrag * currentSpeed);
        
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
    /// Increase speed by specified amount (clamped to acceleration limit and maxSpeed).
    /// </summary>
    public void IncreaseSpeed(float speedIncrease)
    {
        if (isDestroyed || !isPowered) return;
        
        float modifiedAccel = GetAcceleration();
        float modifiedMaxSpeed = GetMaxSpeed();
        
        // Clamp speed increase to acceleration limit
        float actualIncrease = Mathf.Min(speedIncrease, modifiedAccel);
        
        float oldSpeed = currentSpeed;
        currentSpeed = Mathf.Min(currentSpeed + actualIncrease, modifiedMaxSpeed);
        
        if (Mathf.Abs(currentSpeed - oldSpeed) > 0.01f && parentVehicle != null)
        {
            RaceHistory.Log(
                Assets.Scripts.Logging.EventType.Movement,
                EventImportance.Low,
                $"{parentVehicle.vehicleName} accelerated: {oldSpeed:F1} → {currentSpeed:F1}",
                parentVehicle.currentStage,
                parentVehicle
            ).WithMetadata("oldSpeed", oldSpeed)
             .WithMetadata("newSpeed", currentSpeed)
             .WithMetadata("maxSpeed", modifiedMaxSpeed);
        }
    }
    
    /// <summary>
    /// Decrease speed by specified amount (clamped to deceleration limit and zero).
    /// </summary>
    public void DecreaseSpeed(float speedDecrease)
    {
        if (isDestroyed) return;
        
        float modifiedDecel = GetDeceleration();
        
        // Clamp speed decrease to deceleration limit
        float actualDecrease = Mathf.Min(speedDecrease, modifiedDecel);
        
        float oldSpeed = currentSpeed;
        currentSpeed = Mathf.Max(currentSpeed - actualDecrease, 0f);
        
        if (Mathf.Abs(currentSpeed - oldSpeed) > 0.01f && parentVehicle != null)
        {
            RaceHistory.Log(
                Assets.Scripts.Logging.EventType.Movement,
                EventImportance.Low,
                $"{parentVehicle.vehicleName} decelerated: {oldSpeed:F1} → {currentSpeed:F1}",
                parentVehicle.currentStage,
                parentVehicle
            ).WithMetadata("oldSpeed", oldSpeed)
             .WithMetadata("newSpeed", currentSpeed);
        }
    }
    
    /// <summary>
    /// Adjusts current speed toward target speed, respecting acceleration/deceleration limits.
    /// Called at start of turn to apply player/AI speed intentions.
    /// Target speed is proportional (0-1), converted to absolute based on current maxSpeed.
    /// Also auto-scales currentSpeed when maxSpeed changes due to buffs/debuffs.
    /// </summary>
    public void AdjustSpeedTowardTarget()
    {
        if (isDestroyed || !isPowered) return;
        
        float currentMaxSpeed = GetMaxSpeed();

        // Auto-scale currentSpeed if maxSpeed changed (buffs/debuffs applied between turns)
        // This ensures speed modifiers affect all vehicles equally regardless of acceleration
        // == I am not a big fan of this solution, but I couldn't find a better way to implement it. ==
        if (lastKnownMaxSpeed > 0.01f && Mathf.Abs(currentMaxSpeed - lastKnownMaxSpeed) > 0.1f)
        {
            float oldSpeed = currentSpeed;
            currentSpeed = currentSpeed * (currentMaxSpeed / lastKnownMaxSpeed);
            currentSpeed = Mathf.Clamp(currentSpeed, 0f, currentMaxSpeed);
            
            if (parentVehicle != null && Mathf.Abs(currentSpeed - oldSpeed) > 0.01f)
            {
                RaceHistory.Log(
                    Assets.Scripts.Logging.EventType.Modifier,
                    EventImportance.Low,
                    $"{parentVehicle.vehicleName}'s speed scaled: {oldSpeed:F1} → {currentSpeed:F1} (maxSpeed: {lastKnownMaxSpeed:F1} → {currentMaxSpeed:F1})",
                    parentVehicle.currentStage,
                    parentVehicle
                ).WithMetadata("oldSpeed", oldSpeed)
                 .WithMetadata("newSpeed", currentSpeed)
                 .WithMetadata("oldMaxSpeed", lastKnownMaxSpeed)
                 .WithMetadata("newMaxSpeed", currentMaxSpeed);
            }
        }
        lastKnownMaxSpeed = currentMaxSpeed;
        
        // Now do normal acceleration/deceleration toward target
        float targetSpeedAbsolute = targetSpeed * currentMaxSpeed;
        float speedDiff = targetSpeedAbsolute - currentSpeed;
        
        if (Mathf.Abs(speedDiff) < 0.01f)
            return; // Already at target
        
        if (speedDiff > 0)
        {
            // Accelerating toward target
            IncreaseSpeed(speedDiff); // Will be clamped to acceleration limit
        }
        else
        {
            // Decelerating toward target
            DecreaseSpeed(-speedDiff); // Will be clamped to deceleration limit
        }
    }
    
    /// <summary>
    /// Set target speed as a proportion of maxSpeed (0.0-1.0).
    /// Called via CustomEffect skills during action phase.
    /// 0.0 = stop, 0.5 = cruise at half speed, 1.0 = full speed.
    /// Automatically adapts to maxSpeed changes (buffs/debuffs).
    /// </summary>
    public void SetTargetSpeed(float proportionalSpeed)
    {
        if (isDestroyed) return;
        
        float oldTarget = targetSpeed;
        targetSpeed = Mathf.Clamp01(proportionalSpeed); // Clamp to 0-1 range
        
        if (Mathf.Abs(targetSpeed - oldTarget) > 0.01f && parentVehicle != null)
        {
            float maxSpeed = GetMaxSpeed();
            float targetAbsolute = targetSpeed * maxSpeed;
            
            RaceHistory.Log(
                Assets.Scripts.Logging.EventType.Movement,
                EventImportance.Low,
                $"{parentVehicle.vehicleName} set target speed: {oldTarget * 100:F0}% → {targetSpeed * 100:F0}% ({targetAbsolute:F1} units/turn)",
                parentVehicle.currentStage,
                parentVehicle
            ).WithMetadata("oldTargetPercent", oldTarget)
             .WithMetadata("newTargetPercent", targetSpeed)
             .WithMetadata("targetAbsolute", targetAbsolute)
             .WithMetadata("currentSpeed", currentSpeed)
             .WithMetadata("maxSpeed", maxSpeed);
        }
    }
    
    
    /// <summary>
    /// Get the stats to display in the UI for this drive component.
    /// Uses StatCalculator for modified values.
    /// </summary>
    public override List<VehicleComponentUI.DisplayStat> GetDisplayStats()
    {
        var stats = new List<VehicleComponentUI.DisplayStat>();
        
        // Get modified values using accessor methods
        float modifiedSpeed = GetMaxSpeed();
        float modifiedAccel = GetAcceleration();
        float modifiedDecel = GetDeceleration();
        float modifiedStab = GetStability();
        float modifiedFriction = GetFriction();
        
        // Core drive stats
        stats.Add(VehicleComponentUI.DisplayStat.WithTooltip("Max Speed", "MSPD", Attribute.MaxSpeed, baseMaxSpeed, modifiedSpeed));
        stats.Add(VehicleComponentUI.DisplayStat.WithTooltip("Acceleration", "ACCEL", Attribute.Acceleration, baseAcceleration, modifiedAccel));
        stats.Add(VehicleComponentUI.DisplayStat.WithTooltip("Deceleration", "DECEL", Attribute.Deceleration, baseDeceleration, modifiedDecel));
        stats.Add(VehicleComponentUI.DisplayStat.WithTooltip("Stability", "STAB", Attribute.Stability, baseStability, modifiedStab));
        
        // Target speed (show as percentage and absolute)
        float targetAbsolute = targetSpeed * modifiedSpeed;
        stats.Add(VehicleComponentUI.DisplayStat.Simple("Target Speed", "TGT", $"{targetSpeed * 100:F0}% ({targetAbsolute:F1})"));
        
        // Physics properties
        stats.Add(VehicleComponentUI.DisplayStat.WithTooltip("Friction", "FRIC", Attribute.BaseFriction, friction, modifiedFriction));
        
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
