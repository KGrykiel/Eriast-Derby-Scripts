using System;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public class CustomEffect : EffectBase
{
    // This UnityEvent can be set up in the Inspector to call any method with these parameters.
    public UnityEvent<Entity, Entity> specialEvent;

    public override void Apply(Entity user, Entity target, UnityEngine.Object context = null, UnityEngine.Object source = null)
    {
        if (specialEvent != null)
            specialEvent.Invoke(user, target);
    }
}
