using UnityEngine;

/// <summary>
/// Chassis component - the structural foundation of a vehicle.
/// MANDATORY: Every vehicle must have exactly one chassis.
/// Provides: HP, AC (Armor Class), and Component Space.
/// Does NOT enable any role (it's purely structural).
/// </summary>
public class ChassisComponent : VehicleComponent
{
    [Header("Chassis Stats")]
    [Tooltip("HP contribution to vehicle total")]
    public int hpBonus = 250;
    
    [Tooltip("AC (Armor Class) contribution to vehicle total")]
    public int acBonus = 22;
    
    [Tooltip("Component Space provided (positive = space for other components)")]
    public int componentSpaceBonus = 2000;
    
    /// <summary>
    /// Called when component is first added or reset in Editor.
    /// Sets default values that appear immediately in Inspector.
    /// </summary>
    void Reset()
    {
        // Set component identity
        componentType = ComponentType.Chassis;
        componentName = "Standard Chassis";
        
        // Set component base stats
        componentHP = 100;  // Chassis is tougher than average
        componentAC = 18;   // Harder to hit
        componentSpaceRequired = 0;  // Chassis provides space, doesn't consume it
        powerDrawPerTurn = 0;  // Passive structure
        
        // Set chassis-specific stats (already have defaults in field declarations)
        // hpBonus = 250;
        // acBonus = 22;
        // componentSpaceBonus = 2000;
        
        // Chassis does NOT enable a role
        enablesRole = false;
        roleName = "";
    }
    
    void Awake()
    {
        // Set component type (in case Reset wasn't called)
        componentType = ComponentType.Chassis;
        
        // Initialize current HP
        currentHP = componentHP;
        
        // Ensure role settings
        enablesRole = false;
        roleName = "";
    }
    
    /// <summary>
    /// Chassis provides HP, AC, and Component Space to the vehicle.
    /// </summary>
    public override VehicleStatModifiers GetStatModifiers()
    {
        // If chassis is destroyed or disabled, it contributes nothing
        if (isDestroyed || isDisabled)
            return VehicleStatModifiers.Zero;
        
        // Create modifiers using the flexible stat system
        var modifiers = new VehicleStatModifiers();
        modifiers.HP = hpBonus;
        modifiers.AC = acBonus;
        modifiers.ComponentSpace = componentSpaceBonus;
        
        return modifiers;
    }
    
    /// <summary>
    /// Called when chassis is destroyed.
    /// This is catastrophic - without a chassis, the vehicle is likely to collapse.
    /// </summary>
    protected override void OnComponentDestroyed()
    {
        base.OnComponentDestroyed();
        
        // Chassis destruction is catastrophic
        Debug.LogError($"[Chassis] CRITICAL: {componentName} destroyed! Vehicle structural integrity compromised!");
        
        // TODO: In future, trigger vehicle destruction if chassis is destroyed
        // For now, vehicle can continue with reduced stats
    }
}
