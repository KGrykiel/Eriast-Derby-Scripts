using UnityEngine;
using Assets.Scripts.Effects;

/// <summary>
/// Used as a representation of any effect that can be applied to an entity like damage, status effects, resource restoration etc.
/// Ideal for no-code usage in the editor.
/// </summary>
[System.Serializable]
public abstract class EffectBase : IEffect
{
    public abstract void Apply(Entity target, EffectContext context, Object source = null);
}
