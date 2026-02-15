using Assets.Scripts.Core;
using Assets.Scripts.Entities;
using Assets.Scripts.Entities.Vehicle;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Movement/propulsion. Enables Driver role.</summary>
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
    private int currentSpeed = 0;

    [SerializeField]
    [Tooltip("Target speed as percentage of maxSpeed (0 = stopped, 100 = full speed). Set by Driver during action phase. INTEGER-FIRST.")]
    [Range(0, 100)]
    private int targetSpeedPercent = 0;

    // Cached maxSpeed to detect changes from buffs/debuffs. Not the cleanest, should think of a better way to handle this.
    private int lastKnownMaxSpeed;
    
    [Header("Mechanical Properties")]
    [SerializeField]
    [Tooltip("Constant friction from drive system in units/turn (rolling resistance, mechanical friction). Typical: 1-3. INTEGER.")]
    private int baseFriction = 2;  // Constant 2 units lost per turn

    /// <summary>
    /// Default values for convenience, to be edited manually.
    /// </summary>
    void Reset()
    {
        gameObject.name = "Drive";
        componentType = ComponentType.Drive;

        baseMaxHealth = 60;
        health = 60;
        baseArmorClass = 16;
        baseComponentSpace = 200;
        basePowerDrawPerTurn = 10;
        roleType = RoleType.Driver;
    }

    void Awake()
    {
        componentType = ComponentType.Drive;
        roleType = RoleType.Driver;
        //vehicle starts stationary.
        currentSpeed = 0;
        lastKnownMaxSpeed = baseMaxSpeed;
    }
    
    // ==================== STAT ACCESSORS ====================

    public int GetBaseMaxSpeed() => baseMaxSpeed;
    public int GetBaseAcceleration() => baseAcceleration;
    public int GetBaseDeceleration() => baseDeceleration;
    public int GetBaseStability() => baseStability;
    public int GetBaseFriction() => baseFriction;

    public int GetMaxSpeed() => StatCalculator.GatherAttributeValue(this, Attribute.MaxSpeed, baseMaxSpeed);
    public int GetAcceleration() => StatCalculator.GatherAttributeValue(this, Attribute.Acceleration, baseAcceleration);
    public int GetDeceleration() => StatCalculator.GatherAttributeValue(this, Attribute.Deceleration, baseDeceleration);
    public int GetStability() => StatCalculator.GatherAttributeValue(this, Attribute.Stability, baseStability);
    public int GetFriction() => StatCalculator.GatherAttributeValue(this, Attribute.BaseFriction, baseFriction);

    public int GetCurrentSpeed() => currentSpeed;
    
    
    
    // ==================== POWER MANAGEMENT ====================

    /// <summary>Power draw scales with speed via physics calculator.</summary>
    public override int GetActualPowerDraw()
    {
        if (!IsOperational) return 0;

        return VehiclePhysicsCalculator.CalculateSpeedPowerCost(
            currentSpeed,
            GetPowerDrawPerTurn(),
            GetFriction(),
            parentVehicle.chassis.GetDragCoefficientPercent()
        );
    }

    /// <summary>Natural deceleration when unpowered. Called before power draw.</summary>
    public void ApplyFriction()
    {
        if (currentSpeed <= 0) return;
        
        int frictionLoss = VehiclePhysicsCalculator.CalculateFrictionLoss(
            currentSpeed,
            GetFriction(),
            parentVehicle.chassis.GetDragCoefficientPercent()
        );
        
        int oldSpeed = currentSpeed;
        currentSpeed = Mathf.Max(0, currentSpeed - frictionLoss);
        
        this.LogSpeedChange(oldSpeed, currentSpeed, "friction", frictionLoss);
    }
    
    // ==================== ACCELERATION SYSTEM ====================

    public void IncreaseSpeed(int speedIncrease)
    {
        if (!IsOperational) return;

        int modifiedAccel = GetAcceleration();
        int modifiedMaxSpeed = GetMaxSpeed();
        int actualIncrease = Mathf.Min(speedIncrease, modifiedAccel);

        int oldSpeed = currentSpeed;
        currentSpeed = Mathf.Min(currentSpeed + actualIncrease, modifiedMaxSpeed);

        this.LogSpeedChange(oldSpeed, currentSpeed, "acceleration");
    }

    public void DecreaseSpeed(int speedDecrease)
    {
        if (isDestroyed) return;

        int modifiedDecel = GetDeceleration();
        int actualDecrease = Mathf.Min(speedDecrease, modifiedDecel);

        int oldSpeed = currentSpeed;
        currentSpeed = Mathf.Max(currentSpeed - actualDecrease, 0);

        this.LogSpeedChange(oldSpeed, currentSpeed, "deceleration");
    }
    
    
    /// <summary>Moves current speed toward targetSpeedPercent, respecting accel/decel limits.</summary>
    public void AdjustSpeedTowardTarget()
    {
        if (!IsOperational) return;

        int currentMaxSpeed = GetMaxSpeed();

        // Auto-scale currentSpeed if maxSpeed changed (buffs/debuffs between turns)
        // Ensures speed modifiers affect all vehicles equally regardless of acceleration
        if (lastKnownMaxSpeed > 0 && currentMaxSpeed != lastKnownMaxSpeed)
        {
            int oldSpeed = currentSpeed;
            currentSpeed = (currentSpeed * currentMaxSpeed) / lastKnownMaxSpeed;
            currentSpeed = Mathf.Clamp(currentSpeed, 0, currentMaxSpeed);

            this.LogSpeedScaling(oldSpeed, currentSpeed, lastKnownMaxSpeed, currentMaxSpeed);
        }
        lastKnownMaxSpeed = currentMaxSpeed;

        int targetSpeedAbsolute = (targetSpeedPercent * currentMaxSpeed) / 100;
        int speedDiff = targetSpeedAbsolute - currentSpeed;

        if (speedDiff == 0)
            return;

        if (speedDiff > 0)
            IncreaseSpeed(speedDiff);
        else
            DecreaseSpeed(-speedDiff);
    }
    
    public void SetTargetSpeed(int speedPercent)
    {
        if (isDestroyed) return;

        int oldTarget = targetSpeedPercent;
        targetSpeedPercent = Mathf.Clamp(speedPercent, 0, 100);

        this.LogTargetSpeedSet(oldTarget, targetSpeedPercent);
    }


    public override List<VehicleComponentUI.DisplayStat> GetDisplayStats()
    {
        var stats = new List<VehicleComponentUI.DisplayStat>();

        int modifiedSpeed = GetMaxSpeed();
        int modifiedAccel = GetAcceleration();
        int modifiedDecel = GetDeceleration();
        int modifiedStab = GetStability();
        int modifiedFriction = GetFriction();

        stats.Add(VehicleComponentUI.DisplayStat.WithTooltip("Max Speed", "MSPD", Attribute.MaxSpeed, baseMaxSpeed, modifiedSpeed));
        stats.Add(VehicleComponentUI.DisplayStat.WithTooltip("Acceleration", "ACCEL", Attribute.Acceleration, baseAcceleration, modifiedAccel));
        stats.Add(VehicleComponentUI.DisplayStat.WithTooltip("Deceleration", "DECEL", Attribute.Deceleration, baseDeceleration, modifiedDecel));
        stats.Add(VehicleComponentUI.DisplayStat.WithTooltip("Stability", "STAB", Attribute.Stability, baseStability, modifiedStab));

        int targetAbsolute = (targetSpeedPercent * modifiedSpeed) / 100;
        stats.Add(VehicleComponentUI.DisplayStat.Simple("Target Speed", "TGT", $"{targetSpeedPercent}% ({targetAbsolute})"));

        stats.Add(VehicleComponentUI.DisplayStat.WithTooltip("Friction", "FRIC", Attribute.BaseFriction, baseFriction, modifiedFriction));

        stats.AddRange(base.GetDisplayStats());
        
        return stats;
    }
}
