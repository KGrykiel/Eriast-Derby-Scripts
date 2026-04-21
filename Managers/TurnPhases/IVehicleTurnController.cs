using System;
using Assets.Scripts.Entities.Vehicles;

namespace Assets.Scripts.Managers.TurnPhases
{
    /// <summary>
    /// Implemented by anything that can control a vehicle's action phase.
    /// The implementor calls onDone when the turn is fully resolved.
    /// </summary>
    public interface IVehicleTurnController
    {
        void BeginTurn(Vehicle vehicle, TurnPhaseContext context, Action onDone);
    }
}
