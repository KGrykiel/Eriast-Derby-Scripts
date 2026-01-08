using UnityEngine;
using RacingGame.Events;

/// <summary>
/// Chassis component - the structural foundation of a vehicle.
/// MANDATORY: Every vehicle must have exactly one chassis.
/// The chassis IS the vehicle - Entity.maxHealth IS the vehicle's max HP, Entity.armorClass IS the vehicle's AC.
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
    
    /// <summary>
    /// Get maximum HP for this chassis (base + bonuses from other components + modifiers).
    /// Uses Entity.maxHealth as the base.
    /// </summary>
    public int GetMaxHP()
    {
        if (parentVehicle == null) return maxHealth;
        
        // Apply component modifiers to base HP
        float modifiedHP = ApplyModifiers(Attribute.MaxHealth, maxHealth);
        
        // Add bonuses from other components
        float componentBonuses = parentVehicle.GetComponentStat(VehicleStatModifiers.StatNames.HP);
        
        return Mathf.RoundToInt(modifiedHP + componentBonuses);
    }
    
    /// <summary>
    /// Get total Armor Class (base + bonuses from other components + modifiers).
    /// Uses Entity.armorClass as the base.
    /// </summary>
    public int GetTotalAC()
    {
        if (parentVehicle == null) return armorClass;
        
        // Apply component modifiers to base AC
        float modifiedAC = ApplyModifiers(Attribute.ArmorClass, armorClass);
        
        // Add bonuses from other components
        float componentBonuses = parentVehicle.GetComponentStat(VehicleStatModifiers.StatNames.AC);
        
        return Mathf.RoundToInt(modifiedAC + componentBonuses);
    }
    
    /// <summary>
    /// Chassis provides Component Space to the vehicle.
    /// Destroyed/disabled chassis provides nothing.
    /// </summary>
    public override VehicleStatModifiers GetStatModifiers()
    {
        // If chassis is destroyed or disabled, it contributes nothing
        if (isDestroyed || isDisabled)
            return VehicleStatModifiers.Zero;
        
        // Chassis only provides component space (as negative value in componentSpace field)
        // HP and AC are accessed directly via GetMaxHP() and GetTotalAC()
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
        
        // Chassis destruction is catastrophic - vehicle is destroyed
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
        
        // Chassis destruction already triggers vehicle destruction via Vehicle.TakeDamage()
        // which calls OnEntityDestroyed() -> DestroyVehicle()
        // No additional logic needed here
    }
}
