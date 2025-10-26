using UnityEngine;
[System.Serializable]
public abstract class EffectBase : IEffect
{
    public abstract void Apply(Entity user, Entity target, Object context = null, Object source = null);
}
