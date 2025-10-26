using System;
using UnityEngine;

[Serializable]
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
        var vehicle = target as Vehicle;
        if (vehicle == null) return;

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
                    SimulationLogger.LogEvent($"{vehicle.vehicleName} {action} {Mathf.Abs(actualHealthChange)} health.");
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
                    SimulationLogger.LogEvent($"{vehicle.vehicleName} {action} {Mathf.Abs(actualEnergyChange)} energy.");
                }
                break;
        }
    }
}
