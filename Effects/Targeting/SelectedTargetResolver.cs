using System;
using System.Collections.Generic;
using Assets.Scripts.Combat.Rolls.RollSpecs;
using SerializeReferenceEditor;

namespace Assets.Scripts.Effects.Targeting
{
    /// <summary>
    /// Resolves to <c>ctx.Target</c> cast as <see cref="IEffectTarget"/>.
    /// Use for player-selected targets and source component selections.
    /// </summary>
    [Serializable]
    [SRName("Selected Target")]
    public class SelectedTargetResolver : IEffectTargetResolver
    {
        public IReadOnlyList<IEffectTarget> Resolve(RollContext ctx)
        {
            if (ctx.Target is IEffectTarget target)
                return new IEffectTarget[] { target };
            return Array.Empty<IEffectTarget>();
        }
    }
}
