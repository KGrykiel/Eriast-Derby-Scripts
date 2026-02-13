using Assets.Scripts.Entities.Vehicle;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Navigator component - strategic support and information gathering system.
/// OPTIONAL: Provides pathfinding, hazard detection, and crew coordination.
/// ENABLES ROLE: "Navigator" - allows a character to perform navigation actions.
/// </summary>
public class NavigatorComponent : VehicleComponent
{
    /// <summary>
    /// Called when component is first added or reset in Editor.
    /// Sets default values that appear immediately in Inspector.
    /// </summary>
    void Reset()
    {
        // Set GameObject name (shows in hierarchy)
        gameObject.name = "Navigator";

        // Set component identity
        componentType = ComponentType.Sensors;

        // Set component base stats using Entity fields
        baseMaxHealth = 50;      // Moderately fragile
        health = 50;         // Start at full HP
        baseArmorClass = 15;     // Somewhat exposed
        baseComponentSpace = 150;  // Consumes component space
        basePowerDrawPerTurn = 5;  // Light power draw

        // Navigator ENABLES the "Navigator" role
        roleType = RoleType.Navigator;
    }

    void Awake()
    {
        // Set component type (in case Reset wasn't called)
        componentType = ComponentType.Sensors;

        // Navigator ENABLES the "Navigator" role
        roleType = RoleType.Navigator;
    }

    /// <summary>
    /// Get the stats to display in the UI for this navigator component.
    /// </summary>
    public override List<VehicleComponentUI.DisplayStat> GetDisplayStats()
    {
        var stats = new List<VehicleComponentUI.DisplayStat>();

        // Add base class stats (power draw)
        stats.AddRange(base.GetDisplayStats());

        return stats;
    }
}
