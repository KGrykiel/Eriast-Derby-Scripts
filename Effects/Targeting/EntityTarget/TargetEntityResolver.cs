using System;
using System.Collections.Generic;
using Assets.Scripts.Combat.Rolls.RollSpecs;
using Assets.Scripts.Effects.Invocations;
using Assets.Scripts.Entities;
using SerializeReferenceEditor;
using UnityEngine;

namespace Assets.Scripts.Effects.Targeting.EntityTarget
{
    /// <summary>
    /// Resolves to the entity from <c>ctx.Target</c>. Strict pass-through — only handles Entity targets.
    /// For vehicle-level targeting use <see cref="VehicleEffectInvocation"/> with a vehicle resolver instead.
    /// </summary>
    [Serializable]
    [SRName("Target")]
    public class TargetEntityResolver : IEntityEffectResolver
    {
        public IReadOnlyList<Entity> Resolve(RollContext ctx)
        {
            if (ctx.Target is not Entity entity)
            {
                Debug.LogWarning($"[TargetEntityResolver] No entity target in context. Causal source: {ctx.CausalSource ?? "unknown"}");
                return Array.Empty<Entity>();
            }

            return new Entity[] { entity };
        }
    }
}
