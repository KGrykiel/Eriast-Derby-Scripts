using Assets.Scripts.Entities.Vehicles;

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
                vehicle.CurrentStage,
                vehicle
            ).WithMetadata("cardName", card.cardName);
        }

        public static void LogCardEvent(this EventCard card, Vehicle vehicle, CardResolutionResult result)
        {
            Logging.RaceHistory.Log(
                Logging.EventType.EventCard,
                card.dramaticWeight,
                result.narrativeOutcome,
                vehicle.CurrentStage,
                vehicle
            ).WithMetadata("cardName", card.cardName)
             .WithMetadata("success", result.success);
        }
    }
}
