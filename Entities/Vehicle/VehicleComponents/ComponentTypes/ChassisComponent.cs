using UnityEngine;
using RacingGame.Events;

/// <summary>
/// Chassis component - the structural foundation of a vehicle.
/// MANDATORY: Every vehicle must have exactly one chassis.
/// The chassis IS the vehicle - Entity.maxHealth IS the vehicle's max HP, Entity.armorClass IS the vehicle's AC.
/// 
/// NOTE: Base values only. Use AttackCalculator.GatherDefenseValue() for AC with modifiers.
/// </summary>
public class ChassisComponent : VehicleComponent
{
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
        componentSpace = -2000; // Provides space (negative value)
        
        // Chassis provides space, doesn't consume it
        powerDrawPerTurn = 0;  // Passive structure
        
        // Chassis does NOT enable a role
        enablesRole = false;
        roleName = "";
    }
    
    void Awake()
    {
        // Set component type (in case Reset wasn't called)
        componentType = ComponentType.Chassis;
        
        // Ensure role settings
        enablesRole = false;
        roleName = "";
    }
    
    // ==================== STATS ====================
    
    /// <summary>
    /// Get maximum HP for this chassis (base + bonuses from other components + modifiers).
    /// </summary>
    public int GetMaxHP()
    {
        if (parentVehicle == null) return maxHealth;
        
        // Apply status effect modifiers
        float modifiedHP = ApplyModifiers(Attribute.MaxHealth, maxHealth);
        
        // Add bonuses from other components (e.g., armor plating)
        float componentBonuses = parentVehicle.GetComponentStat(VehicleStatModifiers.StatNames.HP);
        
        return Mathf.RoundToInt(modifiedHP + componentBonuses);
    }
    
    /// <summary>
    /// Chassis provides Component Space to the vehicle.
    /// Destroyed/disabled chassis provides nothing.
    /// </summary>
    public override VehicleStatModifiers GetStatModifiers()
    {
        if (isDestroyed || isDisabled)
            return VehicleStatModifiers.Zero;
        
        // Chassis only provides component space (as negative value in componentSpace field)
        // HP and AC are accessed directly via GetMaxHP() and GetArmorClass()
        return VehicleStatModifiers.Zero;
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
        
        RacingGame.Events.RaceHistory.Log(
            RacingGame.Events.EventType.Combat,
            RacingGame.Events.EventImportance.Critical,
            $"[CRITICAL] {parentVehicle.vehicleName}'s Chassis destroyed! Vehicle structural collapse imminent!",
            parentVehicle.currentStage,
            parentVehicle
        ).WithMetadata("componentName", name)
         .WithMetadata("componentType", "Chassis")
         .WithMetadata("catastrophicFailure", true);
    }
}
