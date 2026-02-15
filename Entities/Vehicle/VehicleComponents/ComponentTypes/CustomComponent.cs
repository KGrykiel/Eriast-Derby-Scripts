/// <summary>
/// Catch-all component type for anything that doesn't fit into the other categories. Could be just flavour.
/// </summary>
public class CustomComponent : VehicleComponent
{
    /// <summary>
    /// Default values for convenience, to be edited manually.
    /// </summary>
    void Reset()
    {
        gameObject.name = "Custom Component";
        componentType = ComponentType.Custom;

        baseMaxHealth = 50;
        health = 50;
        baseArmorClass = 15;
        baseComponentSpace = 100;
        basePowerDrawPerTurn = 0;
        roleType = RoleType.None;
    }
}
