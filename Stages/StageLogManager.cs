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
                $"{LogColors.Vehicle(vehicle.vehicleName)} triggered event card: {cardName} in {stage.stageName}",
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
                $"{LogColors.Vehicle(vehicle.vehicleName)} left {stage.stageName}",
                stage,
                vehicle
            );
        }

        public static void LogLaneTurnEffect(
            this Stage stage,
            Vehicle vehicle,
            StageLane lane,
            bool success)
        {
            if (vehicle == null || lane == null) return;

            string outcome = success
                ? $"<color={LogColors.Success}>survived</color>"
                : $"<color={LogColors.Failure}>was hit by</color>";
            string narrative = $"{LogColors.Vehicle(vehicle.vehicleName)} {outcome} {lane.laneName}";

            RaceHistory.Log(
                EventType.StageHazard,
                EventImportance.Medium,
                narrative,
                stage,
                vehicle
            );
        }

        public static void LogStageTurnEffect(this Stage stage, Vehicle vehicle, bool success)
        {
            if (vehicle == null) return;

            string outcome = success
                ? $"<color={LogColors.Success}>survived</color>"
                : $"<color={LogColors.Failure}>was hit by</color>";
            string narrative = $"{LogColors.Vehicle(vehicle.vehicleName)} {outcome} {stage.stageName}";

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

