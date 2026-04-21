namespace Assets.Scripts.Managers.TurnPhases
{
    /// <summary>
    /// Routes the action phase to whichever IVehicleTurnController is
    /// registered for the current vehicle's ControlType.
    /// All per-vehicle logic (operational checks, events) lives in the controllers.
    /// </summary>
    public class ActionHandler : ITurnPhaseHandler
    {
        public TurnPhase Phase => TurnPhase.Action;

        public TurnPhase? Execute(TurnPhaseContext context)
        {
            IVehicleTurnController controller = context.CurrentController;

            if (controller == null)
            {
                UnityEngine.Debug.LogError($"[ActionHandler] No controller registered for {context.CurrentVehicle?.controlType}");
                return TurnPhase.TurnEnd;
            }

            controller.BeginTurn(context.CurrentVehicle, context, () => context.StateMachine.Resume(context, TurnPhase.TurnEnd));
            return null;
        }
    }
}
