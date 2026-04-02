using System;
using System.Collections.Generic;
using Assets.Scripts.Combat.Rolls.RollSpecs;
using Assets.Scripts.Entities.Vehicles.VehicleComponents;
using SerializeReferenceEditor;
using UnityEngine;

namespace Assets.Scripts.Effects.Targeting
{
    /// <summary>
    /// Resolves to the component that is acting (the skill's source component).
    /// Expects <c>ctx.SourceActor.GetEntity()</c> to be a <see cref="VehicleComponent"/>.
    /// </summary>
    [Serializable]
    [SRName("Entity/Source Component")]
    public class SourceComponentResolver : IEffectTargetResolver
    {
        public IReadOnlyList<IEffectTarget> Resolve(RollContext ctx)
        {
            VehicleComponent component = ctx.SourceActor != null ? ctx.SourceActor.GetEntity() as VehicleComponent : null;
            if (component != null)
                return new IEffectTarget[] { component };
            Debug.LogWarning("[SourceComponentResolver] No source component in context.");
            return Array.Empty<IEffectTarget>();
        }
    }
}
