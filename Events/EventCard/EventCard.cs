using System.Collections.Generic;
using UnityEngine;
namespace Assets.Scripts.Events.EventCard
{
    /// <summary>
    /// Base class for event cards that create narrative moments, challenges,
    /// and tactical decisions during races.
    /// 
    /// Concrete implementations:
    /// - HazardCard: Simple skill check with success/failure effects
    /// - ChoiceCard: Player chooses from 2-4 options
    /// - MultiRoleCard: All 5 roles tested simultaneously
    /// </summary>
    public abstract class EventCard : ScriptableObject
    {
        [Header("Card Identity")]
        [Tooltip("Display name for this event card")]
        public string cardName = "Unnamed Event";
        
        [TextArea(3, 6)]
        [Tooltip("Narrative text the DM reads aloud when this card triggers")]
        public string narrativeText = "An event occurs!";
        
        [Tooltip("How dramatic/important this event is for logging")]
        public Logging.EventImportance dramaticWeight = Logging.EventImportance.Medium;
        
        [Header("Targeting")]
        [Tooltip("Who is affected by this card?")]
        public CardTargetMode targetMode = CardTargetMode.DrawingVehicle;
        
        // ==================== ABSTRACT METHODS ====================
        
        /// <summary>
        /// Resolves this card for a player vehicle.
        /// May pause for player input (choices, confirmations).
        /// </summary>
        public abstract CardResolutionResult Resolve(Vehicle vehicle, Stage stage);
        
        /// <summary>
        /// Auto-resolves this card for an NPC vehicle.
        /// Must not pause execution (AI makes decisions instantly).
        /// For simple cards, this can just call Resolve().
        /// For complex cards (choices), AI evaluates options first.
        /// </summary>
        public abstract CardResolutionResult AutoResolve(Vehicle vehicle, Stage stage);
        
        // ==================== TRIGGER METHOD ====================
        
        /// <summary>
        /// Main entry point called by Stage when card is drawn.
        /// Routes to Resolve() or AutoResolve() based on vehicle type.
        /// </summary>
        public void Trigger(Vehicle vehicle, Stage stage)
        {
            if (vehicle == null || stage == null)
            {
                Debug.LogError($"[EventCard] Cannot trigger {cardName}: vehicle or stage is null");
                return;
            }
            
            // Route to player or NPC resolution
            // TODO: isPlayerVehicle property doesn't exist yet
            // For now, always use regular Resolve (assume player)
            bool isPlayer = true; // vehicle.isPlayerVehicle;
            
            CardResolutionResult result = isPlayer 
                ? Resolve(vehicle, stage) 
                : AutoResolve(vehicle, stage);
            
            // Log to race history if dramatic
            if (result != null && result.IsDramatic())
            {
                LogCardEvent(vehicle, stage, result);
            }
        }
        
        // ==================== HELPER METHODS ====================
        
        /// <summary>
        /// Applies a list of effects to a vehicle.
        /// Used by concrete card types to resolve success/failure effects.
        /// </summary>
        protected void ApplyEffects(List<EffectInvocation> effects, Vehicle vehicle)
        {
            // TODO: Effect application needs proper integration
            // For now, just log that effects would be applied
            if (effects != null && effects.Count > 0)
            {
                Debug.LogWarning($"[EventCard] Effect application not yet implemented for {effects.Count} effects on {cardName}");
            }
        }
        
        /// <summary>
        /// Logs this card event to RaceHistory for DM review.
        /// </summary>
        protected void LogCardEvent(Vehicle vehicle, Stage stage, CardResolutionResult result)
        {
            string logText = $"{vehicle.vehicleName}: {narrativeText}\n{result.narrativeOutcome}";
            
            Logging.RaceHistory.Log(
                Logging.EventType.EventCard,
                dramaticWeight,
                logText,
                stage,
                vehicle
            ).WithMetadata("cardName", cardName)
             .WithMetadata("success", result.success);
        }
    }
}
