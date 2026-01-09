using System.Collections.Generic;
using System.Linq;
using RacingGame.Events;
using EventType = RacingGame.Events.EventType;
using Assets.Scripts.Skills.Helpers;

namespace Assets.Scripts.Skills.Helpers
{
    /// <summary>
    /// Handles all combat logging for skills.
    /// Logs attacks, damage, misses, and component targeting results.
    /// </summary>
    public static class SkillCombatLogger
    {
        /// <summary>
        /// Logs skill roll miss to combat log.
        /// </summary>
        public static void LogSkillMiss(string skillName, Vehicle user, Vehicle mainTarget, VehicleComponent sourceComponent, RollBreakdown skillRoll)
        {
            string attackerName = user.vehicleName;
            string sourceText = sourceComponent != null ? $" with {sourceComponent.name}" : "";
            
            var missEvt = RaceHistory.Log(
                EventType.Combat,
                EventImportance.Medium,
                $"{attackerName} attacks {mainTarget.vehicleName}{sourceText}. <color=#FF4444>Miss</color>",
                user.currentStage,
                user, mainTarget
            );
            
            missEvt.WithMetadata("skillName", skillName)
                   .WithMetadata("result", "miss")
                   .WithMetadata("rollBreakdown", skillRoll?.ToDetailedString() ?? "");
        }
        
        /// <summary>
        /// Logs skill roll hit to combat log.
        /// </summary>
        public static void LogSkillHit(string skillName, Vehicle user, Vehicle mainTarget, VehicleComponent sourceComponent, RollBreakdown skillRoll)
        {
            string attackerName = user.vehicleName;
            string sourceText = sourceComponent != null ? $" with {sourceComponent.name}" : "";
            
            var hitEvt = RaceHistory.Log(
                EventType.Combat,
                EventImportance.High,
                $"{attackerName} attacks {mainTarget.vehicleName}{sourceText}. <color=#44FF44>Hit</color>",
                user.currentStage,
                user, mainTarget
            );
            
            hitEvt.WithMetadata("skillName", skillName)
                  .WithMetadata("result", "hit")
                  .WithMetadata("rollBreakdown", skillRoll?.ToDetailedString() ?? "");
        }
        
        /// <summary>
        /// Logs damage results for each target.
        /// </summary>
        public static void LogDamageResults(string skillName, Vehicle user, Dictionary<Entity, List<DamageBreakdown>> damageByTarget)
        {
            foreach (var kvp in damageByTarget)
            {
                Entity target = kvp.Key;
                List<DamageBreakdown> breakdowns = kvp.Value;
                
                if (breakdowns.Count == 0) continue;
                
                int totalDamage = breakdowns.Sum(d => d.finalDamage);
                string attackerName = user.vehicleName;
                
                // Determine target name
                Vehicle targetVehicle = EntityHelpers.GetParentVehicle(target);
                string targetName = targetVehicle != null ? targetVehicle.vehicleName : EntityHelpers.GetEntityDisplayName(target);
                
                // Check if this is self-targeting (healing/self-damage)
                bool isSelfTarget = targetVehicle == user;
                string actionVerb = isSelfTarget ? "takes" : "deals";
                string preposition = isSelfTarget ? "" : $" to {targetName}";
                
                var damageEvt = RaceHistory.Log(
                    EventType.Combat,
                    EventImportance.High,
                    $"{attackerName} {actionVerb} <color=#FFA500>{totalDamage}</color> damage{preposition}",
                    user.currentStage,
                    user, targetVehicle
                );
                
                damageEvt.WithMetadata("skillName", skillName)
                        .WithMetadata("damage", totalDamage)
                        .WithMetadata("damageSource", skillName)
                        .WithMetadata("isSelfTarget", isSelfTarget);
                
                // Build combined damage breakdown
                string combinedBreakdown = SkillDamageFormatter.BuildCombinedDamageBreakdown(breakdowns, skillName);
                damageEvt.WithMetadata("damageBreakdown", combinedBreakdown);
            }
        }
        
        /// <summary>
        /// Logs component hit and damage.
        /// </summary>
        public static void LogComponentHit(
            string skillName,
            Vehicle user, 
            Vehicle mainTarget, 
            string targetComponentName,
            VehicleComponent sourceComponent,
            RollBreakdown componentRoll, 
            Dictionary<Entity, List<DamageBreakdown>> damageByTarget)
        {
            string attackerName = user.vehicleName;
            string sourceText = sourceComponent != null ? $" with {sourceComponent.name}" : "";
            
            var attackEvt = RaceHistory.Log(
                EventType.Combat,
                EventImportance.High,
                $"{attackerName} attacks {mainTarget.vehicleName}'s {targetComponentName}{sourceText}. <color=#44FF44>Hit</color>",
                user.currentStage,
                user, mainTarget
            );
            
            attackEvt.WithMetadata("skillName", skillName)
                    .WithMetadata("targetComponent", targetComponentName)
                    .WithMetadata("result", "hit")
                    .WithMetadata("rollBreakdown", componentRoll.ToDetailedString());
            
            // Log damage
            LogComponentDamage(skillName, user, mainTarget, targetComponentName, damageByTarget, false);
        }
        
        /// <summary>
        /// Logs component miss.
        /// </summary>
        public static void LogComponentMiss(
            string skillName,
            Vehicle user, 
            Vehicle mainTarget, 
            string targetComponentName,
            VehicleComponent sourceComponent,
            RollBreakdown componentRoll)
        {
            string attackerName = user.vehicleName;
            string sourceText = sourceComponent != null ? $" with {sourceComponent.name}" : "";
            
            var componentMissEvt = RaceHistory.Log(
                EventType.Combat,
                EventImportance.Medium,
                $"{attackerName} attacks {mainTarget.vehicleName}'s {targetComponentName}{sourceText}. <color=#FF4444>Miss</color>",
                user.currentStage,
                user, mainTarget
            );
            
            componentMissEvt.WithMetadata("skillName", skillName)
                           .WithMetadata("targetComponent", targetComponentName)
                           .WithMetadata("result", "miss")
                           .WithMetadata("rollBreakdown", componentRoll.ToDetailedString());
        }
        
        /// <summary>
        /// Logs chassis hit and damage.
        /// </summary>
        public static void LogChassisHit(
            string skillName,
            Vehicle user, 
            Vehicle mainTarget, 
            string targetComponentName,
            VehicleComponent sourceComponent,
            RollBreakdown chassisRoll, 
            Dictionary<Entity, List<DamageBreakdown>> damageByTarget)
        {
            string attackerName = user.vehicleName;
            string sourceText = sourceComponent != null ? $" with {sourceComponent.name}" : "";
            
            var chassisAttackEvt = RaceHistory.Log(
                EventType.Combat,
                EventImportance.Medium,
                $"{attackerName} attacks {mainTarget.vehicleName}'s chassis{sourceText}. <color=#44FF44>Hit</color>",
                user.currentStage,
                user, mainTarget
            );
            
            chassisAttackEvt.WithMetadata("skillName", skillName)
                           .WithMetadata("targetComponent", "chassis")
                           .WithMetadata("result", "hit")
                           .WithMetadata("rollBreakdown", chassisRoll.ToDetailedString());
            
            // Log damage
            LogComponentDamage(skillName, user, mainTarget, targetComponentName, damageByTarget, true);
        }
        
        /// <summary>
        /// Logs chassis miss.
        /// </summary>
        public static void LogChassisMiss(string skillName, Vehicle user, Vehicle mainTarget, VehicleComponent sourceComponent, RollBreakdown chassisRoll)
        {
            string attackerName = user.vehicleName;
            string sourceText = sourceComponent != null ? $" with {sourceComponent.name}" : "";
            
            var chassisMissEvt = RaceHistory.Log(
                EventType.Combat,
                EventImportance.Medium,
                $"{attackerName} attacks {mainTarget.vehicleName}'s chassis{sourceText}. <color=#FF4444>Miss</color>",
                user.currentStage,
                user, mainTarget
            );
            
            chassisMissEvt.WithMetadata("skillName", skillName)
                         .WithMetadata("targetComponent", "chassis")
                         .WithMetadata("result", "miss")
                         .WithMetadata("rollBreakdown", chassisRoll.ToDetailedString());
        }
        
        /// <summary>
        /// Logs damage for component-targeted attacks.
        /// </summary>
        public static void LogComponentDamage(
            string skillName,
            Vehicle user, 
            Vehicle mainTarget, 
            string targetComponentName,
            Dictionary<Entity, List<DamageBreakdown>> damageByTarget, 
            bool isChassisHit)
        {
            string attackerName = user.vehicleName;
            
            foreach (var kvp in damageByTarget)
            {
                Entity target = kvp.Key;
                List<DamageBreakdown> breakdowns = kvp.Value;
                int totalDamage = breakdowns.Sum(d => d.finalDamage);
                
                // Determine target name
                Vehicle targetVehicle = EntityHelpers.GetParentVehicle(target);
                string targetName = targetVehicle != null ? targetVehicle.vehicleName : EntityHelpers.GetEntityDisplayName(target);
                
                // Check if this is self-targeting
                bool isSelfTarget = targetVehicle == user;
                
                // Build appropriate log message
                string damageLog;
                if (isSelfTarget)
                {
                    damageLog = $"{attackerName} takes <color=#FFA500>{totalDamage}</color> damage";
                }
                else if (targetVehicle == mainTarget)
                {
                    string componentName = isChassisHit ? "chassis" : targetComponentName;
                    damageLog = $"{attackerName} deals <color=#FFA500>{totalDamage}</color> damage to {mainTarget.vehicleName}'s {componentName}";
                }
                else
                {
                    damageLog = $"{attackerName} deals <color=#FFA500>{totalDamage}</color> damage to {targetName}";
                }
                
                var damageEvt = RaceHistory.Log(
                    EventType.Combat,
                    isChassisHit ? EventImportance.Medium : EventImportance.High,
                    damageLog,
                    user.currentStage,
                    user, targetVehicle ?? mainTarget
                );
                
                damageEvt.WithMetadata("skillName", skillName)
                        .WithMetadata("targetComponent", isChassisHit ? "chassis" : targetComponentName)
                        .WithMetadata("damage", totalDamage)
                        .WithMetadata("damageSource", skillName)
                        .WithMetadata("isSelfTarget", isSelfTarget);
                
                string combinedBreakdown = SkillDamageFormatter.BuildCombinedDamageBreakdown(breakdowns, skillName);
                damageEvt.WithMetadata("damageBreakdown", combinedBreakdown);
            }
        }
    }
}
