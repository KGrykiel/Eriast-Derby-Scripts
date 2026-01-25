using Assets.Scripts.Effects;
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
            
            // Route to player or NPC resolution based on control type
            bool isPlayer = vehicle.controlType == ControlType.Player;
            
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
        /// Vehicle's routing logic determines which component receives each effect based on effect type.
        /// </summary>
        protected void ApplyEffects(List<EffectInvocation> effects, Vehicle vehicle)
        {
            if (effects == null || effects.Count == 0) return;
            
            foreach (var invocation in effects)
            {
                if (invocation.effect == null)
                {
                    Debug.LogWarning($"[EventCard] Null effect in {cardName} effect list");
                    continue;
                }
                
                // Route effect to appropriate target
                // Use Auto precision - vehicle determines best component based on effect type
                Entity targetEntity = vehicle.RouteEffectTarget(
                    invocation.effect, 
                    TargetPrecision.Auto, 
                    null);
                
                if (targetEntity == null)
                {
                    Debug.LogWarning($"[EventCard] Failed to route effect target for {cardName}");
                    continue;
                }
                
                // Apply effect:
                // - user: null (environmental effect - no attacker/caster)
                // - target: routed component (determined by effect type and vehicle routing)
                // - context: null (no special combat state)
                // - source: this event card (for tracking)
                invocation.effect.Apply(
                    user: null,          // No user (environmental effect)
                    target: targetEntity, // Target = routed by vehicle
                    context: EffectContext.Default,        // No special context needed
                    source: this);        // Source = this card
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
