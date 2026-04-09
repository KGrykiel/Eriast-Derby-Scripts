using Assets.Scripts.Entities.Vehicles;
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

            this.LogCardTrigger(vehicle);

            bool isPlayer = vehicle.controlType == ControlType.Player;
            if (isPlayer)
            {
                Resolve(vehicle);
            }
            else
            {
                CardResolutionResult result = AutoResolve(vehicle);
                if (result != null)
                    this.LogCardEvent(vehicle, result);
            }
        }
    }
}
