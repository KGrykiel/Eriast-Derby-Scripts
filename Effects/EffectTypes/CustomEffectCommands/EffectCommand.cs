using UnityEngine;

namespace Assets.Scripts.Effects.EffectTypes.CustomEffectCommands
{
    /// <summary>Base class for effect commands - used for custom effects that are not common enough to warrant a whole category.</summary>
    public abstract class EffectCommand : ScriptableObject
    {
        public abstract void Execute(Entity user, Entity target, EffectContext context, object source);
    }
}

