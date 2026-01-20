using UnityEngine;
using System.Collections.Generic;
using Assets.Scripts.Logging;
using Assets.Scripts.Core;

/// <summary>
/// Chassis component - the structural foundation of a vehicle.
/// MANDATORY: Every vehicle must have exactly one chassis.
/// The chassis IS the vehicle - Entity.maxHealth IS the vehicle's max HP, Entity.armorClass IS the vehicle's AC.
/// 
/// NOTE: Base values only. Use StatCalculator.GatherDefenseValue() for AC with modifiers.
/// </summary>
public class ChassisComponent : VehicleComponent
{
    [Header("Chassis Stats")]
    [Tooltip("Base mobility for saving throws (dodging, evasion). Higher = easier to dodge AOE/traps.")]
    public int baseMobility = 8;
    
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
        maxHealth = 100;      // This IS the vehicle's max HP
        health = 100;         // Start at full HP
        armorClass = 18;      // This IS the vehicle's AC
        baseMobility = 8;     // Default mobility for saves
        componentSpace = -2000; // Provides space (negative value)
        
        // Chassis provides space, doesn't consume it
        powerDrawPerTurn = 0;  // Passive structure
        
        // Chassis does NOT enable a role
        enablesRole = false;
        roleType = RoleType.None;
    }
    
    void Awake()
    {
        // Set component type (in case Reset wasn't called)
        componentType = ComponentType.Chassis;
        
        // Ensure role settings
        enablesRole = false;
        roleType = RoleType.None;
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
        int baseSpace = -componentSpace;
        float modifiedSpace = -StatCalculator.GatherAttributeValue(this, Attribute.ComponentSpace, componentSpace);
        if (baseSpace > 0 || modifiedSpace > 0)
        {
            stats.Add(VehicleComponentUI.DisplayStat.WithTooltip("Capacity", "CAP", Attribute.ComponentSpace, baseSpace, modifiedSpace));
        }

        stats.Add(VehicleComponentUI.DisplayStat.WithTooltip("Mobility", "MBL", Attribute.Mobility, baseMobility, StatCalculator.GatherAttributeValue(this, Attribute.Mobility, baseMobility)));
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
        
        Debug.LogError($"[Chassis] CRITICAL: {parentVehicle.vehicleName}'s {name} destroyed! Vehicle structure collapsed!");
        
        RaceHistory.Log(
            Assets.Scripts.Logging.EventType.Combat,
            EventImportance.Critical,
            $"[CRITICAL] {parentVehicle.vehicleName}'s Chassis destroyed! Vehicle structural collapse imminent!",
            parentVehicle.currentStage,
            parentVehicle
        ).WithMetadata("componentName", name)
         .WithMetadata("componentType", "Chassis")
         .WithMetadata("catastrophicFailure", true);
    }
}
