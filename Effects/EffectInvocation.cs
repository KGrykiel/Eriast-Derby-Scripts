using UnityEngine;
using SerializeReferenceEditor;

/// <summary>
/// The full wrapper that is used in the Unity editor. Allows to route each effect in a skill/event to different targets
/// for example, damage to selected target but speed buff to self. Vehicle.RouteEffectTarget() handles the routing based on EffectTarget and effect type.
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
/// Target of the particular effect.
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
}
