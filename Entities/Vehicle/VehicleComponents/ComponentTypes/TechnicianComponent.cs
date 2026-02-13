using Assets.Scripts.Entities.Vehicle;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Technician component - maintenance, power management, and emergency repair system.
/// OPTIONAL: Provides repair capabilities and power optimization.
/// ENABLES ROLE: "Technician" - allows a character to perform maintenance actions.
/// </summary>
public class TechnicianComponent : VehicleComponent
{
    /// <summary>
    /// Called when component is first added or reset in Editor.
    /// Sets default values that appear immediately in Inspector.
    /// </summary>
    void Reset()
    {
        // Set GameObject name (shows in hierarchy)
        gameObject.name = "Technician";

        // Set component identity
        componentType = ComponentType.Utility;

        // Set component base stats using Entity fields
        baseMaxHealth = 55;      // Moderately durable
        health = 55;         // Start at full HP
        baseArmorClass = 16;     // Reasonably protected
        baseComponentSpace = 175;  // Consumes component space
        basePowerDrawPerTurn = 7;  // Moderate power draw

        // Technician ENABLES the "Technician" role
        roleType = RoleType.Technician;
    }

    void Awake()
    {
        // Set component type (in case Reset wasn't called)
        componentType = ComponentType.Utility;

        // Technician ENABLES the "Technician" role
        roleType = RoleType.Technician;
    }

    /// <summary>
    /// Get the stats to display in the UI for this technician component.
    /// </summary>
    public override List<VehicleComponentUI.DisplayStat> GetDisplayStats()
    {
        var stats = new List<VehicleComponentUI.DisplayStat>();

        // Add base class stats (power draw)
        stats.AddRange(base.GetDisplayStats());

        return stats;
    }
}
