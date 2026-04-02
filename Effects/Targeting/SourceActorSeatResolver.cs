using System;
using System.Collections.Generic;
using Assets.Scripts.Combat.Rolls.RollSpecs;
using Assets.Scripts.Entities.Vehicles;
using SerializeReferenceEditor;

namespace Assets.Scripts.Effects.Targeting
{
    /// <summary>
    /// Resolves to the seat occupied by the rolling character (<c>ctx.SourceActor</c>).
    /// Returns empty if the actor is not seated (e.g. vehicle-level actors).
    /// </summary>
    [Serializable]
    [SRName("Seat/Source Actor")]
    public class SourceActorSeatResolver : IEffectTargetResolver
    {
        public IReadOnlyList<IEffectTarget> Resolve(RollContext ctx)
        {
            VehicleSeat seat = ctx.SourceActor != null ? ctx.SourceActor.GetSeat() : null;
            if (seat != null)
                return new IEffectTarget[] { seat };
            return Array.Empty<IEffectTarget>();
        }
    }
}
