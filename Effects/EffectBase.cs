using UnityEngine;
using Assets.Scripts.Effects;

/// <summary>
/// Abstract base class for all effects.
/// Subclasses implement Apply() to define specific effect behavior.
/// Use EntityHelpers for common entity operations (GetParentVehicle, GetEntityDisplayName, etc.)
/// </summary>
[System.Serializable]
public abstract class EffectBase : IEffect
{
    public abstract void Apply(Entity user, Entity target, EffectContext context, Object source = null);
}
