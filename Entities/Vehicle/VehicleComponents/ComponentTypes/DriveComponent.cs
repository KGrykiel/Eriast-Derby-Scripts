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
    [Tooltip("Maximum speed this drive can achieve in units/turn (base value before modifiers). INTEGER: D&D-style discrete movement.")]
    private int baseMaxSpeed = 40;
    
    [SerializeField]
    [Tooltip("Maximum speed increase per turn (base value before modifiers). INTEGER: Discrete acceleration.")]
    private int baseAcceleration = 5;
    
    [SerializeField]
    [Tooltip("Maximum speed decrease per turn when braking (base value before modifiers). INTEGER: Discrete deceleration.")]
    private int baseDeceleration = 10;
    
    [SerializeField]
    [Tooltip("Stability - resistance to terrain effects and bumps (base value before modifiers). INTEGER.")]
    private int baseStability = 5;
    
    [Header("Speed Management")]
    [Tooltip("Current actual speed in units/turn. INTEGER: D&D-style discrete position.")]
    [ReadOnly]
    private int currentSpeed = 0;

    [SerializeField]
    [Tooltip("Target speed as proportion of maxSpeed (0.0 = stopped, 1.0 = full speed). Set by Driver during action phase.")]
    [Range(0f, 1.0f)]
    [ReadOnly]
    private float targetSpeed = 0f;
    
    // Cached maxSpeed to detect changes from buffs/debuffs
    private int lastKnownMaxSpeed;
    
    [Header("Mechanical Properties")]
    [SerializeField]
    [Tooltip("Mechanical friction from drive system as percentage (100 = 1.0 normal). Higher = more friction. INTEGER-FIRST.")]
    private int frictionPercent = 100;  // 100 = 1.0 friction
    
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
        
        // Initialize speed to 0 (vehicle starts stationary) - INTEGER
        currentSpeed = 0;
        lastKnownMaxSpeed = baseMaxSpeed;
    }
    
    // Base value accessors (return raw field values without modifiers)
    public int GetBaseMaxSpeed() => baseMaxSpeed;
    public int GetBaseAcceleration() => baseAcceleration;
    public int GetBaseDeceleration() => baseDeceleration;
    public int GetBaseStability() => baseStability;
    public int GetBaseFrictionPercent() => frictionPercent;
    
    // Modified value accessors (return values with all modifiers applied via StatCalculator)
    public int GetMaxSpeed() => StatCalculator.GatherAttributeValue(this, Attribute.MaxSpeed, baseMaxSpeed);
    public int GetAcceleration() => StatCalculator.GatherAttributeValue(this, Attribute.Acceleration, baseAcceleration);
    public int GetDeceleration() => StatCalculator.GatherAttributeValue(this, Attribute.Deceleration, baseDeceleration);
    public int GetStability() => StatCalculator.GatherAttributeValue(this, Attribute.Stability, baseStability);
    public int GetFrictionPercent() => StatCalculator.GatherAttributeValue(this, Attribute.BaseFriction, frictionPercent);
    
    /// <summary>
    /// Get friction as actual ratio (for physics calculations).
    /// Converts percentage to decimal: 100 -> 1.0
    /// </summary>
    public float GetFrictionRatio() => GetFrictionPercent() / 100f;
    
    // Runtime state accessor (not a stat, just current value)
    public int GetCurrentSpeed() => currentSpeed;
    
    
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
        if (!IsOperational) return 0;
        
        // Get friction as ratio (convert from percentage)
        float modifiedFriction = GetFrictionRatio();
        
        // Get chassis drag coefficient as ratio
        float vehicleDrag = 0.1f; // Default fallback
        if (parentVehicle != null && parentVehicle.chassis != null)
        {
            vehicleDrag = parentVehicle.chassis.GetDragCoefficientRatio();
        }
        
        // Physics: Power needed to overcome friction
        // modifiedFriction = mechanical (drive-specific, affected by terrain/maintenance)
        // vehicleDrag * speed = aerodynamic (chassis-specific, modified by components)
        float frictionForce = modifiedFriction + (vehicleDrag * currentSpeed);
        
        // Get base power with modifiers using accessor method
        int basePower = GetPowerDrawPerTurn();
        
        int totalCost = Mathf.RoundToInt(basePower + frictionForce);
        return Mathf.Max(0, totalCost);
    }
    
    /// <summary>
    /// Apply natural friction when drive is unpowered (destroyed/disabled).
    /// Called at start of turn BEFORE power draw.
    /// friction_loss = baseFriction + (chassis.dragCoefficient * currentSpeed)
    /// Vehicle gradually decelerates to a stop.
    /// INTEGER: Rounds friction to int (discrete deceleration).
    /// </summary>
    public void ApplyFriction()
    {
        if (currentSpeed <= 0) return;
        
        // Get friction as ratio (convert from percentage)
        float modifiedFriction = GetFrictionRatio();
        
        // Get chassis drag coefficient as ratio
        float vehicleDrag = 0.1f; // Default fallback
        if (parentVehicle != null && parentVehicle.chassis != null)
        {
            vehicleDrag = parentVehicle.chassis.GetDragCoefficientRatio();
        }
        
        // Physics: friction = constant mechanical + aerodynamic drag
        float frictionLossFloat = modifiedFriction + (vehicleDrag * currentSpeed);
        int frictionLoss = Mathf.RoundToInt(frictionLossFloat);  // Round to int (D&D discrete)
        
        int oldSpeed = currentSpeed;
        currentSpeed = Mathf.Max(0, currentSpeed - frictionLoss);
        
        if (currentSpeed != oldSpeed && parentVehicle != null)
        {
            RaceHistory.Log(
                Assets.Scripts.Logging.EventType.Movement,
                EventImportance.Low,
                $"{parentVehicle.vehicleName} slowed by friction: {oldSpeed} → {currentSpeed}",
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
    /// INTEGER: Discrete speed changes (D&D style).
    /// </summary>
    public void IncreaseSpeed(int speedIncrease)
    {
        if (!IsOperational) return;
        
        int modifiedAccel = GetAcceleration();
        int modifiedMaxSpeed = GetMaxSpeed();
        
        // Clamp speed increase to acceleration limit
        int actualIncrease = Mathf.Min(speedIncrease, modifiedAccel);
        
        int oldSpeed = currentSpeed;
        currentSpeed = Mathf.Min(currentSpeed + actualIncrease, modifiedMaxSpeed);
        
        if (currentSpeed != oldSpeed && parentVehicle != null)
        {
            RaceHistory.Log(
                Assets.Scripts.Logging.EventType.Movement,
                EventImportance.Low,
                $"{parentVehicle.vehicleName} accelerated: {oldSpeed} → {currentSpeed}",
                parentVehicle.currentStage,
                parentVehicle
            ).WithMetadata("oldSpeed", oldSpeed)
             .WithMetadata("newSpeed", currentSpeed)
             .WithMetadata("maxSpeed", modifiedMaxSpeed);
        }
    }
    
    /// <summary>
    /// Decrease speed by specified amount (clamped to deceleration limit and zero).
    /// INTEGER: Discrete speed changes (D&D style).
    /// </summary>
    public void DecreaseSpeed(int speedDecrease)
    {
        if (isDestroyed) return;
        
        int modifiedDecel = GetDeceleration();
        
        // Clamp speed decrease to deceleration limit
        int actualDecrease = Mathf.Min(speedDecrease, modifiedDecel);
        
        int oldSpeed = currentSpeed;
        currentSpeed = Mathf.Max(currentSpeed - actualDecrease, 0);
        
        if (currentSpeed != oldSpeed && parentVehicle != null)
        {
            RaceHistory.Log(
                Assets.Scripts.Logging.EventType.Movement,
                EventImportance.Low,
                $"{parentVehicle.vehicleName} decelerated: {oldSpeed} → {currentSpeed}",
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
    /// INTEGER: Discrete acceleration/deceleration (D&D style).
    /// </summary>
    public void AdjustSpeedTowardTarget()
    {
        if (!IsOperational) return;
        
        int currentMaxSpeed = GetMaxSpeed();

        // Auto-scale currentSpeed if maxSpeed changed (buffs/debuffs applied between turns)
        // This ensures speed modifiers affect all vehicles equally regardless of acceleration
        if (lastKnownMaxSpeed > 0 && currentMaxSpeed != lastKnownMaxSpeed)
        {
            int oldSpeed = currentSpeed;
            // Scale proportionally, round to int
            currentSpeed = Mathf.RoundToInt(currentSpeed * (currentMaxSpeed / (float)lastKnownMaxSpeed));
            currentSpeed = Mathf.Clamp(currentSpeed, 0, currentMaxSpeed);
            
            if (parentVehicle != null && currentSpeed != oldSpeed)
            {
                RaceHistory.Log(
                    Assets.Scripts.Logging.EventType.Modifier,
                    EventImportance.Low,
                    $"{parentVehicle.vehicleName}'s speed scaled: {oldSpeed} → {currentSpeed} (maxSpeed: {lastKnownMaxSpeed} → {currentMaxSpeed})",
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
        int targetSpeedAbsolute = Mathf.RoundToInt(targetSpeed * currentMaxSpeed);
        int speedDiff = targetSpeedAbsolute - currentSpeed;
        
        if (speedDiff == 0)
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
    /// INTEGER-FIRST: All stats are integers.
    /// </summary>
    public override List<VehicleComponentUI.DisplayStat> GetDisplayStats()
    {
        var stats = new List<VehicleComponentUI.DisplayStat>();
        
        // Get modified values using accessor methods (all integers)
        int modifiedSpeed = GetMaxSpeed();
        int modifiedAccel = GetAcceleration();
        int modifiedDecel = GetDeceleration();
        int modifiedStab = GetStability();
        int modifiedFriction = GetFrictionPercent();
        
        // Core drive stats
        stats.Add(VehicleComponentUI.DisplayStat.WithTooltip("Max Speed", "MSPD", Attribute.MaxSpeed, baseMaxSpeed, modifiedSpeed));
        stats.Add(VehicleComponentUI.DisplayStat.WithTooltip("Acceleration", "ACCEL", Attribute.Acceleration, baseAcceleration, modifiedAccel));
        stats.Add(VehicleComponentUI.DisplayStat.WithTooltip("Deceleration", "DECEL", Attribute.Deceleration, baseDeceleration, modifiedDecel));
        stats.Add(VehicleComponentUI.DisplayStat.WithTooltip("Stability", "STAB", Attribute.Stability, baseStability, modifiedStab));
        
        // Target speed (show as percentage and absolute)
        int targetAbsolute = Mathf.RoundToInt(targetSpeed * modifiedSpeed);
        stats.Add(VehicleComponentUI.DisplayStat.Simple("Target Speed", "TGT", $"{targetSpeed * 100:F0}% ({targetAbsolute})"));
        
        // Physics properties (displayed as percentage)
        stats.Add(VehicleComponentUI.DisplayStat.WithTooltip("Friction", "FRIC", Attribute.BaseFriction, frictionPercent, modifiedFriction, "%"));
        
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
