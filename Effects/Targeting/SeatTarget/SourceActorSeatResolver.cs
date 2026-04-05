using System;
using System.Collections.Generic;
using Assets.Scripts.Combat.Rolls.RollSpecs;
using Assets.Scripts.Entities.Vehicles;
using SerializeReferenceEditor;

namespace Assets.Scripts.Effects.Targeting.SeatTarget
{
    /// <summary>
    /// Resolves to the seat occupied by the rolling character (<c>ctx.SourceActor</c>).
    /// Returns empty if the actor is not seated (e.g. vehicle-level actors).
    /// </summary>
    [Serializable]
    [SRName("Source Actor")]
    public class SourceActorSeatResolver : ISeatEffectResolver
    {
        public IReadOnlyList<VehicleSeat> Resolve(RollContext ctx)
        {
            VehicleSeat seat = ctx.SourceActor?.GetSeat();
            if (seat != null)
                return new VehicleSeat[] { seat };
            return Array.Empty<VehicleSeat>();
        }
    }
}
