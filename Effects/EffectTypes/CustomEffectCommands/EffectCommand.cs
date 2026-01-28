using UnityEngine;

namespace Assets.Scripts.Effects.EffectTypes.CustomEffectCommands
{
    /// <summary>
    /// Abstract base for reusable effect commands.
    /// Create ScriptableObject subclasses to implement custom logic that can be referenced in prefab skills.
    /// Commands receive full context (user, target, effect context) and can perform any custom behavior.
    /// </summary>
    public abstract class EffectCommand : ScriptableObject
    {
        /// <summary>
        /// Execute the command with full context.
        /// Implement custom logic in subclasses.
        /// </summary>
        /// <param name="user">Entity using the skill/effect</param>
        /// <param name="target">Primary target entity</param>
        /// <param name="context">Full effect context with vehicles, components, etc.</param>
        /// <param name="source">Source effect (can be CustomEffect for parameter reading)</param>
        public abstract void Execute(Entity user, Entity target, EffectContext context, object source);
    }
}

