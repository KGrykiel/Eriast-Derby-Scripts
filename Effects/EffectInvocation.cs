using UnityEngine;
using SerializeReferenceEditor;

/// <summary>
/// Wraps an effect with explicit component-aware targeting.
/// Supports single targets, multi-targets (AOE), and automatic component routing.
/// The final target entity is determined by Vehicle.RouteEffectTarget() based on effect type.
/// </summary>
[System.Serializable]
public class EffectInvocation
{
    [SerializeReference, SR]
    public IEffect effect;

    [Tooltip("Who receives this effect?")]
    public EffectTarget target = EffectTarget.SelectedTarget;
}

/// <summary>
/// Explicit targeting system with automatic component routing.
/// Vehicle.RouteEffectTarget() determines the final entity based on effect type:
/// - DamageEffect ? Chassis
/// - AttributeModifierEffect(Speed) ? Drive
/// - AttributeModifierEffect(Energy) ? PowerCore
/// </summary>
public enum EffectTarget
{
    // Single targets - Source
    [Tooltip("The component using this skill (e.g., weapon that fires, power core that overloads)")]
    SourceComponent,
    
    [Tooltip("The user's vehicle (routes based on effect type: damage?chassis, speed?drive, energy?power core)")]
    SourceVehicle,
    
    [Tooltip("Player-selected component on user's vehicle (shows component selection UI)")]
    SourceComponentSelection,
    
    // Single targets - Selected target
    [Tooltip("Player-selected target (respects manual component targeting from UI if used)")]
    SelectedTarget,
    
    [Tooltip("Target vehicle (routes based on effect type, ignores manual component selection)")]
    TargetVehicle,
    
    // Multi-targets
    [Tooltip("Both source and target vehicles (each routed based on effect type)")]
    Both,
    
    [Tooltip("All enemy vehicles in stage (each routed based on effect type)")]
    AllEnemiesInStage,
    
    [Tooltip("All allied vehicles in stage including self (each routed based on effect type)")]
    AllAlliesInStage
}
