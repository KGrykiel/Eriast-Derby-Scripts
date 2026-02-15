using System.Collections.Generic;

/// <summary>
/// Strategic support. Enables Navigator role.
/// TODO: refine navigator role and abilities..
/// </summary>
public class NavigatorComponent : VehicleComponent
{
    /// <summary>
    /// Default values for convenience, to be edited manually.
    /// </summary>
    void Reset()
    {
        gameObject.name = "Navigator";
        componentType = ComponentType.Sensors;

        baseMaxHealth = 50;
        health = 50;
        baseArmorClass = 15;
        baseComponentSpace = 150;
        basePowerDrawPerTurn = 5;
        roleType = RoleType.Navigator;
    }

    void Awake()
    {
        componentType = ComponentType.Sensors;
        roleType = RoleType.Navigator;
    }

    public override List<VehicleComponentUI.DisplayStat> GetDisplayStats()
    {
        var stats = new List<VehicleComponentUI.DisplayStat>();

        stats.AddRange(base.GetDisplayStats());

        return stats;
    }
}
