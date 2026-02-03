using UnityEngine;

namespace Assets.Scripts.Managers.TurnPhases
{
    /// <summary>
    /// Handles AIAction phase - AI executes movement and actions synchronously.
    /// Stage transitions are handled by TurnEndHandler (same for both player and AI).
    /// Immediately advances to TurnEnd when complete.
    /// </summary>
    public class AIActionHandler : ITurnPhaseHandler
    {
        public TurnPhase Phase => TurnPhase.AIAction;
        
        public TurnPhase? Execute(TurnPhaseContext context)
        {
            var vehicle = context.CurrentVehicle;
            
            if (vehicle != null && vehicle.IsOperational())
            {
                // Execute movement (emits events via TurnEventBus)
                context.TurnController.ExecuteMovement(vehicle);
                
                // Stage transitions handled by TurnEndHandler (common for player + AI)
                
                // Future: AI skill usage, targeting decisions, etc.
            }
            
            return TurnPhase.TurnEnd;
        }
    }
}
