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
                
                // Determine target name (including component if applicable)
                Vehicle targetVehicle = EntityHelpers.GetParentVehicle(target);
                string targetName;
                
                if (target is VehicleComponent component)
                {
                    // Target is a specific component - show vehicle + component name
                    string vehicleName = targetVehicle?.vehicleName ?? "Unknown Vehicle";
                    targetName = $"{vehicleName}'s {component.name}";
                }
                else
                {
                    // Target is vehicle chassis or other entity
                    targetName = targetVehicle != null ? targetVehicle.vehicleName : EntityHelpers.GetEntityDisplayName(target);
                }
                
                // Check if this is self-targeting (healing/self-damage)
                bool isSelfTarget = targetVehicle == user;
                
                // Build log message
                string damageLog;
                if (isSelfTarget)
                {
                    // Self-damage: Show component name if applicable
                    if (target is VehicleComponent)
                    {
                        damageLog = $"{attackerName}'s {((VehicleComponent)target).name} takes <color=#FFA500>{totalDamage}</color> damage";
                    }
                    else
                    {
                        damageLog = $"{attackerName} takes <color=#FFA500>{totalDamage}</color> damage";
                    }
                }
                else
                {
                    // Damage to other target
                    damageLog = $"{attackerName} deals <color=#FFA500>{totalDamage}</color> damage to {targetName}";
                }
                
                var damageEvt = RaceHistory.Log(
                    EventType.Combat,
                    EventImportance.High,
                    damageLog,
                    user.currentStage,
                    user, targetVehicle
                );
                
                damageEvt.WithMetadata("skillName", skillName)
                        .WithMetadata("damage", totalDamage)
                        .WithMetadata("damageSource", skillName)
                        .WithMetadata("isSelfTarget", isSelfTarget);
                
                // Add component info if target is a component
                if (target is VehicleComponent comp)
                {
                    damageEvt.WithMetadata("targetComponent", comp.name);
                }
                
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
                
                // Determine target vehicle
                Vehicle targetVehicle = EntityHelpers.GetParentVehicle(target);
                
                // Check if this is self-targeting
                bool isSelfTarget = targetVehicle == user;
                
                // Build appropriate log message
                string damageLog;
                if (isSelfTarget)
                {
                    // Self-damage: Show component name if applicable
                    if (target is VehicleComponent selfComponent)
                    {
                        damageLog = $"{attackerName}'s {selfComponent.name} takes <color=#FFA500>{totalDamage}</color> damage";
                    }
                    else
                    {
                        damageLog = $"{attackerName} takes <color=#FFA500>{totalDamage}</color> damage";
                    }
                }
                else if (targetVehicle == mainTarget)
                {
                    // Damage to the main target
                    if (target is VehicleComponent targetComponent)
                    {
                        // Show component name for component damage
                        damageLog = $"{attackerName} deals <color=#FFA500>{totalDamage}</color> damage to {mainTarget.vehicleName}'s {targetComponent.name}";
                    }
                    else
                    {
                        // Show chassis for chassis damage
                        string componentName = isChassisHit ? "chassis" : targetComponentName;
                        damageLog = $"{attackerName} deals <color=#FFA500>{totalDamage}</color> damage to {mainTarget.vehicleName}'s {componentName}";
                    }
                }
                else
                {
                    // Damage to a third party (AOE, etc.)
                    string targetName = targetVehicle != null ? targetVehicle.vehicleName : EntityHelpers.GetEntityDisplayName(target);
                    if (target is VehicleComponent otherComponent)
                    {
                        targetName = $"{targetVehicle?.vehicleName ?? "Unknown Vehicle"}'s {otherComponent.name}";
                    }
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
                        .WithMetadata("damage", totalDamage)
                        .WithMetadata("damageSource", skillName)
                        .WithMetadata("isSelfTarget", isSelfTarget);
                
                // Add component info if target is a component
                if (target is VehicleComponent comp)
                {
                    damageEvt.WithMetadata("targetComponent", comp.name);
                }
                else
                {
                    damageEvt.WithMetadata("targetComponent", isChassisHit ? "chassis" : targetComponentName);
                }
                
                string combinedBreakdown = SkillDamageFormatter.BuildCombinedDamageBreakdown(breakdowns, skillName);
                damageEvt.WithMetadata("damageBreakdown", combinedBreakdown);
            }
        }
        
        // ==================== MODIFIER LOGGING ====================
        
        /// <summary>
        /// Logs modifiers applied to entities.
        /// Groups modifiers by target and logs them as a single entry per target.
        /// </summary>
        public static void LogModifierResults(string skillName, Vehicle user, Dictionary<Entity, List<AttributeModifier>> modifiersByTarget)
        {
            foreach (var kvp in modifiersByTarget)
            {
                Entity target = kvp.Key;
                List<AttributeModifier> modifiers = kvp.Value;
                
                if (modifiers.Count == 0) continue;
                
                // Determine target name
                Vehicle targetVehicle = EntityHelpers.GetParentVehicle(target);
                string targetName;
                
                if (target is VehicleComponent component)
                {
                    string vehicleName = targetVehicle?.vehicleName ?? "Unknown Vehicle";
                    targetName = $"{vehicleName}'s {component.name}";
                }
                else
                {
                    targetName = targetVehicle != null ? targetVehicle.vehicleName : EntityHelpers.GetEntityDisplayName(target);
                }
                
                // Check if self-targeting
                bool isSelfTarget = targetVehicle == user;
                
                // Build modifier list string
                List<string> modifierDescriptions = new List<string>();
                foreach (var mod in modifiers)
                {
                    // NOTE: Duration tracking removed - will be handled by StatusEffect system in Phase 2
                    // For now, show all modifiers as permanent (equipment-style)
                    string modDesc = $"{mod.Type} {mod.Attribute} {mod.Value:+0;-0} (permanent)";
                    modifierDescriptions.Add(modDesc);
                }
                
                string modifierList = string.Join(", ", modifierDescriptions);
                
                // Build log message
                string logMessage;
                if (isSelfTarget)
                {
                    if (target is VehicleComponent)
                    {
                        logMessage = $"{user.vehicleName}'s {((VehicleComponent)target).name} gains: {modifierList}";
                    }
                    else
                    {
                        logMessage = $"{user.vehicleName} gains: {modifierList}";
                    }
                }
                else
                {
                    logMessage = $"{targetName} gains: {modifierList}";
                }
                
                // Determine importance
                EventImportance importance = isSelfTarget ? EventImportance.Medium : EventImportance.High;
                
                var modEvt = RaceHistory.Log(
                    EventType.Modifier,
                    importance,
                    logMessage,
                    user.currentStage,
                    user, targetVehicle
                );
                
                modEvt.WithMetadata("skillName", skillName)
                      .WithMetadata("modifierCount", modifiers.Count)
                      .WithMetadata("isSelfTarget", isSelfTarget);
                
                // Add individual modifier metadata
                for (int i = 0; i < modifiers.Count; i++)
                {
                    var mod = modifiers[i];
                    modEvt.WithMetadata($"modifier_{i}_attribute", mod.Attribute.ToString())
                          .WithMetadata($"modifier_{i}_type", mod.Type.ToString())
                          .WithMetadata($"modifier_{i}_value", mod.Value);
                    // Duration metadata removed - will be tracked by StatusEffect system in Phase 2
                }
            }
        }
        
        // ==================== RESTORATION LOGGING ====================
        
        /// <summary>
        /// Logs resource restoration/drain results for each target.
        /// Handles health and energy restoration with proper formatting.
        /// </summary>
        public static void LogRestorationResults(string skillName, Vehicle user, Dictionary<Entity, List<RestorationBreakdown>> restorationByTarget)
        {
            foreach (var kvp in restorationByTarget)
            {
                Entity target = kvp.Key;
                List<RestorationBreakdown> breakdowns = kvp.Value;
                
                if (breakdowns.Count == 0) continue;
                
                // Determine target name
                Vehicle targetVehicle = EntityHelpers.GetParentVehicle(target);
                string targetName;
                
                if (target is VehicleComponent component)
                {
                    string vehicleName = targetVehicle?.vehicleName ?? "Unknown Vehicle";
                    targetName = $"{vehicleName}'s {component.name}";
                }
                else
                {
                    targetName = targetVehicle != null ? targetVehicle.vehicleName : EntityHelpers.GetEntityDisplayName(target);
                }
                
                // Check if self-targeting
                bool isSelfTarget = targetVehicle == user;
                
                // Group by resource type
                var healthBreakdowns = breakdowns.Where(b => b.resourceType == ResourceRestorationEffect.ResourceType.Health).ToList();
                var energyBreakdowns = breakdowns.Where(b => b.resourceType == ResourceRestorationEffect.ResourceType.Energy).ToList();
                
                // Build restoration message
                List<string> restorationParts = new List<string>();
                
                if (healthBreakdowns.Count > 0)
                {
                    int totalHealthChange = healthBreakdowns.Sum(b => b.actualChange);
                    if (totalHealthChange != 0)
                    {
                        string healthAction = totalHealthChange > 0 ? "restores" : "drains";
                        restorationParts.Add($"{healthAction} <color=#44FF44>{System.Math.Abs(totalHealthChange)}</color> HP");
                    }
                }
                
                if (energyBreakdowns.Count > 0)
                {
                    int totalEnergyChange = energyBreakdowns.Sum(b => b.actualChange);
                    if (totalEnergyChange != 0)
                    {
                        string energyAction = totalEnergyChange > 0 ? "restores" : "drains";
                        restorationParts.Add($"{energyAction} <color=#88DDFF>{System.Math.Abs(totalEnergyChange)}</color> energy");
                    }
                }
                
                if (restorationParts.Count == 0) continue;
                
                // Build log message
                string restorationText = string.Join(" and ", restorationParts);
                string logMessage;
                
                if (isSelfTarget)
                {
                    logMessage = $"{user.vehicleName} {restorationText}";
                }
                else
                {
                    logMessage = $"{user.vehicleName} {restorationText} for {targetName}";
                }
                
                // Determine importance based on context
                EventImportance importance = DetermineRestorationImportance(user, targetVehicle, isSelfTarget, breakdowns);
                
                var restorationEvt = RaceHistory.Log(
                    EventType.Resource,
                    importance,
                    logMessage,
                    user.currentStage,
                    user, targetVehicle
                );
                
                restorationEvt.WithMetadata("skillName", skillName)
                             .WithMetadata("isSelfTarget", isSelfTarget)
                             .WithMetadata("restorationCount", breakdowns.Count);
                
                // Add detailed breakdown metadata
                foreach (var breakdown in breakdowns)
                {
                    string prefix = breakdown.resourceType.ToString().ToLower();
                    restorationEvt.WithMetadata($"{prefix}_actualChange", breakdown.actualChange)
                                  .WithMetadata($"{prefix}_newValue", breakdown.newValue)
                                  .WithMetadata($"{prefix}_maxValue", breakdown.maxValue)
                                  .WithMetadata($"{prefix}_wasClamped", breakdown.WasClamped);
                }
            }
        }
        
        /// <summary>
        /// Determines importance of restoration event based on context.
        /// </summary>
        private static EventImportance DetermineRestorationImportance(Vehicle user, Vehicle target, bool isSelfTarget, List<RestorationBreakdown> breakdowns)
        {
            // Player-involved restorations are more important
            bool playerInvolved = user.controlType == ControlType.Player || (target != null && target.controlType == ControlType.Player);
            
            // Check for critical health restoration
            foreach (var breakdown in breakdowns)
            {
                if (breakdown.resourceType == ResourceRestorationEffect.ResourceType.Health)
                {
                    float healthPercent = breakdown.NewPercentage;
                    
                    // Healing from critical health is important
                    if (breakdown.actualChange > 0 && healthPercent < 0.3f && playerInvolved)
                        return EventImportance.High;
                    
                    // Large health changes
                    if (System.Math.Abs(breakdown.actualChange) > 20)
                        return playerInvolved ? EventImportance.High : EventImportance.Medium;
                }
            }
            
            // Default based on player involvement
            return playerInvolved ? EventImportance.Medium : EventImportance.Low;
        }
    }
}
