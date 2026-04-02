using System;
using System.Collections.Generic;
using Assets.Scripts.Combat.Rolls.RollSpecs;
using Assets.Scripts.Entities;
using Assets.Scripts.Entities.Vehicles;
using SerializeReferenceEditor;
using UnityEngine;

namespace Assets.Scripts.Effects.Targeting
{
    /// <summary>
    /// Resolves to all seats on the chosen vehicle.
    /// Use <see cref="VehicleSource"/> to select the source or target vehicle.
    /// </summary>
    [Serializable]
    [SRName("Seat/All On Vehicle")]
    public class AllSeatsOnVehicleResolver : IEffectTargetResolver
    {
        public VehicleSource source = VehicleSource.TargetVehicle;

        public IReadOnlyList<IEffectTarget> Resolve(RollContext ctx)
        {
            Vehicle vehicle = ResolveVehicle(ctx);
            if (vehicle == null)
            {
                Debug.LogWarning($"[AllSeatsOnVehicleResolver] No {source} in context.");
                return Array.Empty<IEffectTarget>();
            }

            var results = new List<IEffectTarget>();
            foreach (var seat in vehicle.seats)
            {
                if (seat != null)
                    results.Add(seat);
            }
            return results;
        }

        private Vehicle ResolveVehicle(RollContext ctx)
        {
            if (source == VehicleSource.SourceVehicle)
            {
                if (ctx.SourceActor != null)
                {
                    Vehicle vehicle = ctx.SourceActor.GetVehicle();
                    if (vehicle != null) return vehicle;
                }
                return ctx.Target as Vehicle;
            }
            return EntityHelpers.GetVehicleFromTarget(ctx.Target);
        }
    }
}
