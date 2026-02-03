using UnityEngine;

namespace Assets.Scripts.Managers.TurnPhases
{
    /// <summary>
    /// Handles AIAction phase - AI executes movement and actions synchronously.
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
                
                // Handle stage transitions (crossroads for AI)
                HandleStageTransitions(context, vehicle);
                
                // Future: AI skill usage, targeting decisions, etc.
            }
            
            return TurnPhase.TurnEnd;
        }
        
        /// <summary>
        /// Handle stage transitions for AI vehicles.
        /// AI chooses randomly at crossroads (TODO: Strategic decision-making via AI.md).
        /// </summary>
        private void HandleStageTransitions(TurnPhaseContext context, Vehicle vehicle)
        {
            if (vehicle == null || vehicle.currentStage == null) return;
            
            while (vehicle.progress >= vehicle.currentStage.length && 
                   vehicle.currentStage.nextStages.Count > 0)
            {
                if (vehicle.currentStage.nextStages.Count == 1)
                {
                    // Single path - always take it
                    context.TurnController.MoveToStage(vehicle, vehicle.currentStage.nextStages[0], isPlayerChoice: false);
                }
                else
                {
                    // Crossroads - AI chooses randomly
                    // TODO: Implement strategic AI decision-making (see AI.md)
                    var chosenStage = vehicle.currentStage.nextStages[
                        Random.Range(0, vehicle.currentStage.nextStages.Count)
                    ];
                    context.TurnController.MoveToStage(vehicle, chosenStage, isPlayerChoice: false);
                }
            }
        }
    }
}
