using System.Collections.Generic;
using Assets.Scripts.Combat.Rolls.RollSpecs;
using Assets.Scripts.Entities;

namespace Assets.Scripts.Combat.Rolls.Targeting
{
    /// <summary>
    /// Resolves which targets a <see cref="RollNode"/> should execute against.
    /// Placed as an optional field on RollNode. Null means "use the current context as-is"
    /// (single execution, no fan-out). Each resolved target gets its own roll and effects.
    /// </summary>
    public interface ITargetResolver
    {
        IReadOnlyList<IRollTarget> ResolveFrom(RollContext ctx);
    }
}
