using System;
using System.Collections.Generic;
using Assets.Scripts.Combat.Rolls.RollSpecs;
using Assets.Scripts.Entities;
using Assets.Scripts.Entities.Vehicles;
using Assets.Scripts.Entities.Vehicles.VehicleComponents;
using SerializeReferenceEditor;

namespace Assets.Scripts.Combat.Rolls.Targeting
{
    /// <summary>
    /// Resolves to seats on a vehicle that have the specified role enabled.
    /// Returns an empty list if no seat has the role — effect is silently skipped.
    /// </summary>
    [Serializable]
    [SRName("Seat By Role")]
    public class SeatByRoleResolver : IRollTargetResolver
    {
        public RoleType Role;
        public SeatSource Source;

        public SeatByRoleResolver() { }
        public SeatByRoleResolver(RoleType role, SeatSource source)
        {
            Role = role;
            Source = source;
        }

        public IReadOnlyList<IRollTarget> ResolveFrom(RollContext ctx)
        {
            Vehicle vehicle = ResolveVehicle(ctx);
            if (vehicle == null)
                return Array.Empty<IRollTarget>();

            var results = new List<IRollTarget>();
            foreach (var seat in vehicle.seats)
            {
                if (seat != null && seat.GetEnabledRoles().HasFlag(Role))
                    results.Add(seat);
            }
            return results;
        }

        private Vehicle ResolveVehicle(RollContext ctx)
        {
            switch (Source)
            {
                case SeatSource.TargetVehicle:
                    return EntityHelpers.GetVehicleFromTarget(ctx.Target);
                case SeatSource.SourceVehicle:
                    Vehicle sv = ctx.SourceActor?.GetVehicle();
                    return sv != null ? sv : ctx.Target as Vehicle;
                default:
                    return null;
            }
        }
    }

    public enum SeatSource
    {
        TargetVehicle,
        SourceVehicle
    }
}
