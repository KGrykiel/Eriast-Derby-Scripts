using UnityEngine;
using System.Collections.Generic;
using Assets.Scripts.Entities;
using Assets.Scripts.Entities.Vehicle;
using Assets.Scripts.Core;

/// <summary>
/// Chassis component - the structural foundation of a vehicle.
/// MANDATORY: Every vehicle must have exactly one chassis.
/// The chassis IS the vehicle - Entity.maxHealth IS the vehicle's max HP, Entity.armorClass IS the vehicle's AC.
/// The chassis also determines the vehicle's SIZE CATEGORY (for balancing modifiers).
/// 
/// NOTE: Base values only. Use StatCalculator.GatherDefenseValue() for AC with modifiers.
/// </summary>
public class ChassisComponent : VehicleComponent
{
    [Header("Chassis Stats")]
    [SerializeField]
    [Tooltip("Base mobility for saving throws (dodging, evasion). Higher = easier to dodge AOE/traps (base value before modifiers).")]
    private int baseMobility = 8;
    
    [Header("Chassis Size")]
    [Tooltip("Size category determines AC, mobility, and speed modifiers. Larger = easier to hit, harder to maneuver.")]
    public VehicleSizeCategory sizeCategory = VehicleSizeCategory.Medium;
    
    [Header("Aerodynamic Properties")]
    [SerializeField]
    [Tooltip("Aerodynamic drag coefficient as percentage (10 = 0.10, 15 = 0.15). Higher = more drag. INTEGER-FIRST.")]
    private int baseDragCoefficientPercent = 10;  // 10 = 0.10 drag
    
    /// <summary>
    /// Called when component is first added or reset in Editor.
    /// Sets default values that appear immediately in Inspector.
    /// </summary>
    void Reset()
    {
        // Set GameObject name (shows in hierarchy)
        gameObject.name = "Chassis";
        
        // Set component identity
        componentType = ComponentType.Chassis;
        
        // Chassis uses Entity fields directly
        baseMaxHealth = 100;      // This IS the vehicle's max HP
        health = 100;         // Start at full HP
        baseArmorClass = 18;      // This IS the vehicle's AC
        baseMobility = 8;     // Default mobility for saves
        baseComponentSpace = -2000; // Provides space (negative value)
        sizeCategory = VehicleSizeCategory.Medium; // Default size
        
        // Chassis provides space, doesn't consume it
        basePowerDrawPerTurn = 0;  // Passive structure
        
        // Chassis does NOT enable a role
        roleType = RoleType.None;
    }
    
    void Awake()
    {
        // Set component type (in case Reset wasn't called)
        componentType = ComponentType.Chassis;
        
        // Ensure role settings
        roleType = RoleType.None;
    }
    
    // ==================== STAT ACCESSORS ====================
    // Naming convention:
    // - GetBaseStat() returns raw field value (no modifiers)
    // - GetStat() returns effective value (with modifiers via StatCalculator)
    // Game code should almost always use GetStat() for gameplay calculations.
    
    // Inherited from Entity: GetCurrentHealth(), GetBaseMaxHealth(), GetMaxHealth(), GetBaseArmorClass(), GetArmorClass()
    
    // Base value accessors (return raw field values without modifiers)
    public int GetBaseMobility() => baseMobility;
    public int GetBaseDragCoefficientPercent() => baseDragCoefficientPercent;
    
    // Modified value accessors (return values with all modifiers applied via StatCalculator)
    public int GetMobility() => StatCalculator.GatherAttributeValue(this, Attribute.Mobility, baseMobility);
    public int GetDragCoefficientPercent() => StatCalculator.GatherAttributeValue(this, Attribute.DragCoefficient, baseDragCoefficientPercent);
    
    // ==================== D20 ROLL BASE VALUES ====================
    
    /// <summary>
    /// Provide base check values for d20 rolls (checks and saves).
    /// Chassis provides base Mobility for dodge/evasion rolls.
    /// Add more check attributes here as chassis features expand (e.g., Stability for balance checks).
    /// </summary>
    public override int GetBaseCheckValue(VehicleCheckAttribute checkAttribute)
    {
        return checkAttribute switch
        {
            VehicleCheckAttribute.Mobility => baseMobility,
            // Future: VehicleCheckAttribute.Stability => baseStability,
            _ => base.GetBaseCheckValue(checkAttribute)
        };
    }
    
    // ==================== STATS ====================
    
    /// <summary>
    /// Get the stats to display in the UI for this chassis.
    /// Uses StatCalculator for modified values.
    /// </summary>
    public override List<VehicleComponentUI.DisplayStat> GetDisplayStats()
    {
        var stats = new List<VehicleComponentUI.DisplayStat>();
        
        // componentSpace is negative for chassis (provides space)
        int baseSpace = -GetBaseComponentSpace();
        int modifiedSpace = -GetComponentSpace();
        if (baseSpace > 0 || modifiedSpace > 0)
        {
            stats.Add(VehicleComponentUI.DisplayStat.WithTooltip("Capacity", "CAP", Attribute.ComponentSpace, baseSpace, modifiedSpace));
        }

        // Use accessor methods for modified values
        int modifiedMobility = GetMobility();
        stats.Add(VehicleComponentUI.DisplayStat.WithTooltip("Mobility", "MBL", Attribute.Mobility, baseMobility, modifiedMobility));
        
        // Aerodynamic properties (displayed as percentage)
        int modifiedDrag = GetDragCoefficientPercent();
        stats.Add(VehicleComponentUI.DisplayStat.WithTooltip("Drag", "DRAG", Attribute.DragCoefficient, baseDragCoefficientPercent, modifiedDrag, "%"));
        
        // Don't add base class stats - chassis doesn't draw power

        return stats;
    }
    
    /// <summary>
    /// Called when chassis is destroyed.
    /// This is catastrophic - chassis destruction means vehicle destruction.
    /// </summary>
    protected override void OnComponentDestroyed()
    {
        base.OnComponentDestroyed();
        
        if (parentVehicle == null) return;
        
        this.LogChassisDestroyed();
        
        // Immediately mark vehicle as destroyed (fires event for immediate handling)
        parentVehicle.MarkAsDestroyed();
    }
}
