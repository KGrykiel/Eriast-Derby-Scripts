using UnityEngine;
using Assets.Scripts.Effects;

public interface IEffect
{
    /// <summary>
    /// Applies this effect to the target entity.
    /// </summary>
    /// <param name="user">The entity causing the effect (e.g., attacker, caster). Null for environmental effects.</param>
    /// <param name="target">The entity receiving the effect.</param>
    /// <param name="context">Combat/situational state (crits, etc.). Use EffectContext.Default for non-combat.</param>
    /// <param name="source">The source of the effect (e.g., Skill, EventCard, Stage, etc.)</param>
    void Apply(Entity user, Entity target, EffectContext context, Object source = null);
}
