using System;
using UnityEngine;
using RacingGame.Events;

[System.Serializable]
public class ResourceRestorationEffect : EffectBase
{
    public enum ResourceType
    {
        Health,
        Energy
        // Add more resource types later
    }

    public ResourceType resourceType = ResourceType.Health;
    public int amount = 0; // Positive to restore, negative to drain

    public override void Apply(Entity user, Entity target, UnityEngine.Object context = null, UnityEngine.Object source = null)
    {
        // Get parent vehicle from the entity (if it's a component)
        Vehicle vehicle = GetParentVehicle(target);
        if (vehicle == null) return;

        string sourceText = source != null ? source.name : "unknown source";

        switch (resourceType)
        {
            case ResourceType.Health:
                int maxHealth = (int)vehicle.GetAttribute(Attribute.MaxHealth);
                int oldHealth = vehicle.health;
                vehicle.health = Mathf.Clamp(vehicle.health + amount, 0, maxHealth);
                int actualHealthChange = vehicle.health - oldHealth;
                
                if (actualHealthChange != 0)
                {
                    string action = actualHealthChange > 0 ? "recovers" : "loses";
                    
                    // Old logging (keep for backwards compatibility)
                    SimulationLogger.LogEvent($"{vehicle.vehicleName} {action} {Mathf.Abs(actualHealthChange)} health.");
                    
                    // Determine importance based on amount
                    EventImportance importance = DetermineHealthRestoreImportance(vehicle, actualHealthChange);
                    
                    RaceHistory.Log(
                        RacingGame.Events.EventType.Resource,
                        importance,
                        $"[HP] {vehicle.vehicleName} {action} {Mathf.Abs(actualHealthChange)} health from {sourceText} ({vehicle.health}/{maxHealth})",
                        vehicle.currentStage,
                        vehicle
                    ).WithMetadata("resourceType", "Health")
                     .WithMetadata("amount", amount)
                     .WithMetadata("actualChange", actualHealthChange)
                     .WithMetadata("oldHealth", oldHealth)
                     .WithMetadata("newHealth", vehicle.health)
                     .WithMetadata("maxHealth", maxHealth)
                     .WithMetadata("source", sourceText)
                     .WithMetadata("isRestore", actualHealthChange > 0);
                }
                break;

            case ResourceType.Energy:
                int maxEnergy = (int)vehicle.GetAttribute(Attribute.MaxEnergy);
                int oldEnergy = vehicle.energy;
                vehicle.energy = Mathf.Clamp(vehicle.energy + amount, 0, maxEnergy);
                int actualEnergyChange = vehicle.energy - oldEnergy;
                
                if (actualEnergyChange != 0)
                {
                    string action = actualEnergyChange > 0 ? "recovers" : "loses";
                    
                    // Old logging (keep for backwards compatibility)
                    SimulationLogger.LogEvent($"{vehicle.vehicleName} {action} {Mathf.Abs(actualEnergyChange)} energy.");
                    
                    // Energy changes are typically less critical than health
                    EventImportance importance = vehicle.controlType == ControlType.Player 
                        ? EventImportance.Low 
                        : EventImportance.Debug;
                    
                    RaceHistory.Log(
                        RacingGame.Events.EventType.Resource,
                        importance,
                        $"[ENERGY] {vehicle.vehicleName} {action} {Mathf.Abs(actualEnergyChange)} energy from {sourceText} ({vehicle.energy}/{maxEnergy})",
                        vehicle.currentStage,
                        vehicle
                    ).WithMetadata("resourceType", "Energy")
                     .WithMetadata("amount", amount)
                     .WithMetadata("actualChange", actualEnergyChange)
                     .WithMetadata("oldEnergy", oldEnergy)
                     .WithMetadata("newEnergy", vehicle.energy)
                     .WithMetadata("maxEnergy", maxEnergy)
                     .WithMetadata("source", sourceText)
                     .WithMetadata("isRestore", actualEnergyChange > 0);
                }
                break;
        }
    }
    
    /// <summary>
    /// Determines importance of health restoration/drain based on context.
    /// </summary>
    private EventImportance DetermineHealthRestoreImportance(Vehicle vehicle, int healthChange)
    {
        // Healing from critical health is important
        float healthPercent = (float)vehicle.health / vehicle.GetAttribute(Attribute.MaxHealth);
        
        if (vehicle.controlType == ControlType.Player)
        {
            // Player healing from low health
            if (healthChange > 0 && healthPercent < 0.3f)
                return EventImportance.High;
            
            // Player taking health drain
            if (healthChange < 0 && Mathf.Abs(healthChange) > 10)
                return EventImportance.Medium;
            
            return EventImportance.Low;
        }
        
        // NPC large health changes
        if (Mathf.Abs(healthChange) > 20)
            return EventImportance.Medium;
        
        return EventImportance.Low;
    }
}
