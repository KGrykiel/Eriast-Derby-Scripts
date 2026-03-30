using System.Collections.Generic;
using Assets.Scripts.Combat.Rolls.RollSpecs;
using Assets.Scripts.Entities;

namespace Assets.Scripts.Combat.Rolls.Targeting
{
    /// <summary>
    /// Null-object resolver — returns the current target as-is for a single execution.
    /// Explicit equivalent of leaving <see cref="RollNode.targetResolver"/> null.
    /// </summary>
    public class CurrentTargetResolver : ITargetResolver
    {
        public IReadOnlyList<IRollTarget> ResolveFrom(RollContext ctx)
        {
            if (ctx.Target != null)
                return new[] { ctx.Target };

            return System.Array.Empty<IRollTarget>();
        }
    }
}
