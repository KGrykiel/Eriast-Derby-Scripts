using System;
using Assets.Scripts.Entities.Vehicles;
using Assets.Scripts.Managers.PlayerUI;
using Assets.Scripts.Managers.Turn.TurnPhases;
using Assets.Scripts.Skills;

namespace Assets.Scripts.Managers.Turn.TurnControllers
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

        public void RequestNextAction(Vehicle vehicle, TurnPhaseContext context, Action<SkillAction> onAction, Action onDone)
        {
            if (vehicle == null || !vehicle.IsOperational())
            {
                string reason = vehicle != null ? vehicle.GetNonOperationalReason() : "No vehicle";
                TurnEventBus.Emit(new PlayerCannotActEvent(vehicle, reason));
                onDone();
                return;
            }

            inputCoordinator.SetCallbacks(onAction, onDone);
            inputCoordinator.BeginTurn(vehicle);
        }
    }
}
