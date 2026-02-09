using Assets.Scripts.Logging;
using Assets.Scripts.Stages.Lanes;
using Assets.Scripts.Combat.SkillChecks;
using Assets.Scripts.Combat.Saves;
using Assets.Scripts.Combat;
using EventType = Assets.Scripts.Logging.EventType;

namespace Assets.Scripts.Stages
{
    /// <summary>
    /// Extension methods for clean, reusable stage logging.
    /// Centralizes stage-related logging patterns with minimal boilerplate.
    /// Follows same pattern as EntityLogManager.
    /// </summary>
    public static class StageLogManager
    {
        // ==================== EVENT CARD LOGGING ====================
        
        /// <summary>
        /// Log event card trigger for a vehicle.
        /// </summary>
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
             .WithMetadata("stageName", stage.stageName)
             .WithShortDescription($"{vehicle.vehicleName}: {cardName}");
        }
        
        // ==================== STAGE ENTRY/EXIT LOGGING ====================
        
        /// <summary>
        /// Log vehicle leaving stage.
        /// </summary>
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

        // ==================== LANE TURN EFFECTS LOGGING ====================
        
        /// <summary>
        /// Log lane turn effect result (success or failure).
        /// </summary>
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
        
        /// <summary>
        /// Log lane turn effect with skill check result.
        /// </summary>
        public static void LogLaneTurnEffectWithCheck(
            this Stage stage,
            Vehicle vehicle,
            StageLane lane,
            LaneTurnEffect effect,
            SkillCheckResult checkResult)
        {
            if (vehicle == null || lane == null || effect == null || checkResult == null) return;
            
            string narrative = checkResult.Succeeded ? effect.successNarrative : effect.failureNarrative;
            
            if (string.IsNullOrEmpty(narrative))
            {
                narrative = $"{vehicle.vehicleName} {(checkResult.Succeeded ? "passed" : "failed")} {effect.effectName} " +
                           $"({checkResult.checkSpec.DisplayName} DC {checkResult.TargetValue}: rolled {checkResult.Total})";
            }
            
            RaceHistory.Log(
                EventType.StageHazard,
                EventImportance.Medium,
                narrative,
                stage,
                vehicle
            ).WithMetadata("laneName", lane.laneName)
             .WithMetadata("effectName", effect.effectName)
             .WithMetadata("success", checkResult.Succeeded)
             .WithMetadata("checkType", checkResult.checkSpec.DisplayName)
             .WithMetadata("dc", checkResult.TargetValue)
             .WithMetadata("roll", checkResult.Total);
        }
        
        /// <summary>
        /// Log lane turn effect with saving throw result.
        /// </summary>
        public static void LogLaneTurnEffectWithSave(
            this Stage stage,
            Vehicle vehicle,
            StageLane lane,
            LaneTurnEffect effect,
            SaveResult saveResult)
        {
            if (vehicle == null || lane == null || effect == null || saveResult == null) return;
            
            string narrative = saveResult.Succeeded ? effect.successNarrative : effect.failureNarrative;
            
            if (string.IsNullOrEmpty(narrative))
            {
                narrative = $"{vehicle.vehicleName} {(saveResult.Succeeded ? "passed" : "failed")} {effect.effectName} " +
                           $"({saveResult.saveSpec.DisplayName} DC {saveResult.TargetValue}: rolled {saveResult.Total})";
            }
            
            RaceHistory.Log(
                EventType.StageHazard,
                EventImportance.Medium,
                narrative,
                stage,
                vehicle
            ).WithMetadata("laneName", lane.laneName)
             .WithMetadata("effectName", effect.effectName)
             .WithMetadata("success", saveResult.Succeeded)
             .WithMetadata("saveType", saveResult.saveSpec.DisplayName)
             .WithMetadata("dc", saveResult.TargetValue)
             .WithMetadata("roll", saveResult.Total);
        }
    }
}
