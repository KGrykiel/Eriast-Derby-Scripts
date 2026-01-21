using UnityEngine;
using Assets.Scripts.Effects;

public interface IEffect
{
    /// <summary>
    /// Applies this effect. User and target are Entities for maximum flexibility and type safety.
    /// </summary>
    /// <param name="user">The entity causing the effect (e.g., attacker, caster). Null for environmental effects.</param>
    /// <param name="target">The entity receiving the effect (e.g., Vehicle, Obstacle, etc.)</param>
    /// <param name="context">Optional EffectContext with combat state (crits) and environmental data (stage modifiers)</param>
    /// <param name="source">The source of the effect (e.g., Skill, EventCard, Stage, etc.)</param>
    void Apply(Entity user, Entity target, EffectContext? context = null, Object source = null);
}
