using Assets.Scripts.Effects;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Events.EventCard
{
    /// <summary>Base class for event cards</summary>
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

        public abstract CardResolutionResult Resolve(Vehicle vehicle);

        /// <summary>AI resolution — must not pause execution.</summary>
        public abstract CardResolutionResult AutoResolve(Vehicle vehicle);

        // ==================== TRIGGER METHOD ====================

        /// <summary>Called by Stage when card is drawn. Routes to Resolve/AutoResolve.</summary>
        public void Trigger(Vehicle vehicle)
        {
            if (vehicle == null)
            {
                Debug.LogError($"[EventCard] Cannot trigger {cardName}: vehicle is null");
                return;
            }

            bool isPlayer = vehicle.controlType == ControlType.Player;

            CardResolutionResult result = isPlayer 
                ? Resolve(vehicle) 
                : AutoResolve(vehicle);

            if (result != null)
            {
                LogCardEvent(vehicle, result);
            }
        }

        // ==================== HELPER METHODS ====================

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

                Entity targetEntity = vehicle.RouteEffectTarget(invocation.effect);

                if (targetEntity == null)
                {
                    Debug.LogWarning($"[EventCard] Failed to route effect target for {cardName}");
                    continue;
                }

                invocation.effect.Apply(
                    user: null,
                    target: targetEntity,
                    context: EffectContext.Default,
                    source: this);
            }
        }

        protected void LogCardEvent(Vehicle vehicle, CardResolutionResult result)
        {
            string logText = $"{vehicle.vehicleName}: {narrativeText}\n{result.narrativeOutcome}";

            Logging.RaceHistory.Log(
                Logging.EventType.EventCard,
                dramaticWeight,
                logText,
                vehicle.currentStage,
                vehicle
            ).WithMetadata("cardName", cardName)
             .WithMetadata("success", result.success);
        }
    }
}
