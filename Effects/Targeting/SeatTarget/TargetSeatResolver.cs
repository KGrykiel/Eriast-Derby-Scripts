using System;
using System.Collections.Generic;
using Assets.Scripts.Combat.Rolls.RollSpecs;
using Assets.Scripts.Entities.Vehicles;
using Assets.Scripts.Entities.Vehicles.VehicleComponents;
using SerializeReferenceEditor;
using UnityEngine;

namespace Assets.Scripts.Effects.Targeting.SeatTarget
{
    /// <summary>
    /// Resolves to the seat derived from <c>ctx.Target</c>.
    /// VehicleSeat passes through; VehicleComponent navigates to the controlling seat.
    /// Replaces the old <c>SelectedTargetResolver</c> for seat-level effects.
    /// </summary>
    [Serializable]
    [SRName("Target")]
    public class TargetSeatResolver : ISeatEffectResolver
    {
        public IReadOnlyList<VehicleSeat> Resolve(RollContext ctx)
        {
            VehicleSeat seat = ctx.Target switch
            {
                VehicleSeat s => s,
                VehicleComponent component => ResolveSeatFromComponent(component),
                _ => null
            };

            if (seat != null)
                return new VehicleSeat[] { seat };

            Debug.LogWarning("[TargetSeatResolver] No seat target in context.");
            return Array.Empty<VehicleSeat>();
        }

        private static VehicleSeat ResolveSeatFromComponent(VehicleComponent component)
        {
            Vehicle vehicle = component.ParentVehicle;
            if (vehicle == null)
            {
                Debug.LogWarning($"[TargetSeatResolver] Component '{component.name}' has no parent vehicle.");
                return null;
            }
            return vehicle.GetSeatForComponent(component);
        }
    }
}
