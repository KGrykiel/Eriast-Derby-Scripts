using System.Collections.Generic;

/// <summary>
/// Maintenance and power management. Enables Technician role.
/// TODO: refine technician role and abilities.
/// </summary>
public class TechnicianComponent : VehicleComponent
{
    /// <summary>
    /// Default values for convenience, to be edited manually.
    /// </summary>
    void Reset()
    {
        gameObject.name = "Technician";
        componentType = ComponentType.Utility;

        baseMaxHealth = 55;
        health = 55;
        baseArmorClass = 16;
        baseComponentSpace = 175;
        basePowerDrawPerTurn = 7;
        roleType = RoleType.Technician;
    }

    void Awake()
    {
        componentType = ComponentType.Utility;
        roleType = RoleType.Technician;
    }

    public override List<VehicleComponentUI.DisplayStat> GetDisplayStats()
    {
        var stats = new List<VehicleComponentUI.DisplayStat>();

        stats.AddRange(base.GetDisplayStats());

        return stats;
    }
}
