using Assets.Scripts.Logging;
using Assets.Scripts.Stages.Lanes;
using Assets.Scripts.Combat.SkillChecks;
using Assets.Scripts.Combat.Saves;
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
            ).WithMetadata("eventCardName", cardName)
             .WithMetadata("stageName", stage.stageName);
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
            ).WithMetadata("stageName", stage.stageName)
             .WithMetadata("vehicleCount", vehicleCount);
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
            ).WithMetadata("laneName", lane.laneName)
             .WithMetadata("effectName", effect.effectName)
             .WithMetadata("success", success);
        }
        
        public static void LogLaneTurnEffectWithCheck(
            this Stage stage,
            Vehicle vehicle,
            StageLane lane,
            LaneTurnEffect effect,
            SkillCheckResult checkResult)
        {
            if (vehicle == null || lane == null || effect == null || checkResult == null) return;
            
            string narrative = checkResult.Roll.Success ? effect.successNarrative : effect.failureNarrative;

            if (string.IsNullOrEmpty(narrative))
            {
                narrative = $"{vehicle.vehicleName} {(checkResult.Roll.Success ? "passed" : "failed")} {effect.effectName} " +
                           $"({checkResult.Spec.DisplayName} DC {checkResult.Roll.TargetValue}: rolled {checkResult.Roll.Total})";
            }

            RaceHistory.Log(
                EventType.StageHazard,
                EventImportance.Medium,
                narrative,
                stage,
                vehicle
            ).WithMetadata("laneName", lane.laneName)
             .WithMetadata("effectName", effect.effectName)
             .WithMetadata("success", checkResult.Roll.Success)
             .WithMetadata("checkType", checkResult.Spec.DisplayName)
             .WithMetadata("dc", checkResult.Roll.TargetValue)
             .WithMetadata("roll", checkResult.Roll.Total);
        }
        
        public static void LogLaneTurnEffectWithSave(
            this Stage stage,
            Vehicle vehicle,
            StageLane lane,
            LaneTurnEffect effect,
            SaveResult saveResult)
        {
            if (vehicle == null || lane == null || effect == null || saveResult == null) return;
            
            string narrative = saveResult.Roll.Success ? effect.successNarrative : effect.failureNarrative;

            if (string.IsNullOrEmpty(narrative))
            {
                narrative = $"{vehicle.vehicleName} {(saveResult.Roll.Success ? "passed" : "failed")} {effect.effectName} " +
                           $"({saveResult.Spec.DisplayName} DC {saveResult.Roll.TargetValue}: rolled {saveResult.Roll.Total})";
            }

            RaceHistory.Log(
                EventType.StageHazard,
                EventImportance.Medium,
                narrative,
                stage,
                vehicle
            ).WithMetadata("laneName", lane.laneName)
             .WithMetadata("effectName", effect.effectName)
             .WithMetadata("success", saveResult.Roll.Success)
             .WithMetadata("saveType", saveResult.Spec.DisplayName)
             .WithMetadata("dc", saveResult.Roll.TargetValue)
             .WithMetadata("roll", saveResult.Roll.Total);
        }
    }
}
