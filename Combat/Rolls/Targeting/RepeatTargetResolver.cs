using System;
using System.Collections.Generic;
using Assets.Scripts.Combat.Rolls.RollSpecs;
using Assets.Scripts.Entities;
using SerializeReferenceEditor;
using UnityEngine;

namespace Assets.Scripts.Combat.Rolls.Targeting
{
    /// <summary>
    /// Repeats the current context target <see cref="hitCount"/> times,
    /// causing the node to make that many independent rolls against the same target.
    /// Use this for burst-fire and multi-hit weapons.
    /// </summary>
    [Serializable]
    [SRName("Repeat Target")]
    public class RepeatTargetResolver : IRollTargetResolver
    {
        [Min(1)]
        [Tooltip("Number of times to repeat the roll against the same target. Each repetition is an independent roll.")]
        public int hitCount = 2;

        public RepeatTargetResolver(int hitCount = 2)
        {
            this.hitCount = hitCount;
        }

        public IReadOnlyList<IRollTarget> ResolveFrom(RollContext ctx)
        {
            if (ctx.Target == null)
                return Array.Empty<IRollTarget>();

            var results = new List<IRollTarget>(hitCount);
            for (int i = 0; i < hitCount; i++)
                results.Add(ctx.Target);

            return results;
        }
    }
}
