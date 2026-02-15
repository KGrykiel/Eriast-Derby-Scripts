using UnityEngine;
using Assets.Scripts.Effects;

public interface IEffect
{
    void Apply(Entity target, EffectContext context, Object source = null);
}
