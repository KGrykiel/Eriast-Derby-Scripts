using Assets.Scripts.Entities.Vehicles;
using Assets.Scripts.Logging;
using Assets.Scripts.Stages.Lanes;
using EventType = Assets.Scripts.Logging.EventType;

namespace Assets.Scripts.Stages
{
    public static class StageLogManager
    {
        public static void LogEventCardTrigger(this Stage stage, Vehicle vehicle, string cardName)
        {
            if (vehicle == null || string.IsNullOrEmpty(cardName)) return;
            
            EventImportance importance = vehicle.controlType == ControlType.Player 
                ? EventImportance.High 
                : EventImportance.Medium;

            RaceHistory.Log(
                EventType.EventCard,
                importance,
                $"{vehicle.vehicleName} triggered event card: {cardName} in {stage.stageName}",
                stage,
                vehicle
            );
        }

        public static void LogStageExit(this Stage stage, Vehicle vehicle, int vehicleCount)
        {
            if (vehicle == null) return;
            
            RaceHistory.Log(
                EventType.Movement,
                EventImportance.Debug,
                $"{vehicle.vehicleName} left {stage.stageName}",
                stage,
                vehicle
            );
        }

        public static void LogLaneTurnEffect(
            this Stage stage, 
            Vehicle vehicle, 
            StageLane lane, 
            LaneTurnEffect effect, 
            bool success)
        {
            if (vehicle == null || lane == null || effect == null) return;

            string narrative = success ? effect.successNarrative : effect.failureNarrative;

            if (string.IsNullOrEmpty(narrative))
                narrative = $"{vehicle.vehicleName} {(success ? "succeeded at" : "failed")} {effect.effectName}";

            RaceHistory.Log(
                EventType.StageHazard,
                EventImportance.Medium,
                narrative,
                stage,
                vehicle
            );
        }
    }
}
