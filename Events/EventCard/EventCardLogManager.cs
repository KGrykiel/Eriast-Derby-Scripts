using Assets.Scripts.Entities.Vehicles;
using Assets.Scripts.Managers;

namespace Assets.Scripts.Events.EventCard
{
    public static class EventCardLogManager
    {
        public static void LogCardTrigger(this EventCard card, Vehicle vehicle)
        {
            Logging.RaceHistory.Log(
                Logging.EventType.EventCard,
                card.dramaticWeight,
                $"{vehicle.vehicleName}: {card.narrativeText}",
                RacePositionTracker.GetStage(vehicle),
                vehicle
            ).WithMetadata("cardName", card.cardName);
        }

        public static void LogCardEvent(this EventCard card, Vehicle vehicle, CardResolutionResult result)
        {
            Logging.RaceHistory.Log(
                Logging.EventType.EventCard,
                card.dramaticWeight,
                result.narrativeOutcome,
                RacePositionTracker.GetStage(vehicle),
                vehicle
            ).WithMetadata("cardName", card.cardName)
             .WithMetadata("success", result.success);
        }
    }
}
