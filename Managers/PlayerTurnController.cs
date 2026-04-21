using System;
using Assets.Scripts.Entities.Vehicles;
using Assets.Scripts.Managers.PlayerUI;
using Assets.Scripts.Managers.TurnPhases;

namespace Assets.Scripts.Managers
{
    /// <summary>
    /// Turn controller for player-controlled vehicles. Wires the action and
    /// completion callbacks into PlayerInputCoordinator, then hands off to the UI.
    /// </summary>
    public class PlayerTurnController : IVehicleTurnController
    {
        private readonly PlayerInputCoordinator inputCoordinator;

        public PlayerTurnController(PlayerInputCoordinator inputCoordinator)
        {
            this.inputCoordinator = inputCoordinator;
        }

        public void BeginTurn(Vehicle vehicle, TurnPhaseContext context, Action onDone)
        {
            if (vehicle == null || !vehicle.IsOperational())
            {
                string reason = vehicle != null ? vehicle.GetNonOperationalReason() : "No vehicle";
                TurnEventBus.Emit(new PlayerCannotActEvent(vehicle, reason));
                onDone();
                return;
            }

            inputCoordinator.SetCallbacks(context.ActionManager.Submit, onDone);
            inputCoordinator.BeginTurn(vehicle);
        }
    }
}
