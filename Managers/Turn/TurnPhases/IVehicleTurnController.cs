using System;
using Assets.Scripts.Entities.Vehicles;
using Assets.Scripts.Skills;

namespace Assets.Scripts.Managers.Turn.TurnPhases
{
    /// <summary>
    /// Implemented by anything that can control a vehicle's action phase.
    /// The manager calls RequestNextAction repeatedly until the controller
    /// signals it is done via onDone.
    /// </summary>
    public interface IVehicleTurnController
    {
        /// <summary>
        /// Request one action from the controller. The controller either calls
        /// onAction with the chosen action, or calls onDone if it has nothing left to do.
        /// </summary>
        void RequestNextAction(Vehicle vehicle, TurnPhaseContext context, Action<SkillAction> onAction, Action onDone);
    }
}
