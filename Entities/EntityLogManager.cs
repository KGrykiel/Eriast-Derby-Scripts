using Assets.Scripts.Logging;
using Assets.Scripts.Entities.Vehicle.VehicleComponents.ComponentTypes;
using EventType = Assets.Scripts.Logging.EventType;

namespace Assets.Scripts.Entities
{
    /// <summary>
    /// Extension methods for clean, reusable entity logging.
    /// Centralizes common logging patterns with minimal boilerplate.
    /// Reduces 10-15 lines of logging code to a single method call.
    /// </summary>
    public static class EntityLogManager
    {
        // ==================== MOVEMENT LOGGING ====================
        
        /// <summary>
        /// Log speed change for a drive component.
        /// Handles all speed change reasons: friction, acceleration, deceleration, scaling.
        /// </summary>
        public static void LogSpeedChange(this DriveComponent drive, int oldSpeed, int newSpeed, string reason, int? amount = null)
        {
            if (oldSpeed == newSpeed || drive.ParentVehicle == null) return;
            
            string message = reason switch
            {
                "friction" => $"{drive.ParentVehicle.vehicleName} slowed by friction: {oldSpeed} → {newSpeed}",
                "acceleration" => $"{drive.ParentVehicle.vehicleName} accelerated: {oldSpeed} → {newSpeed}",
                "deceleration" => $"{drive.ParentVehicle.vehicleName} decelerated: {oldSpeed} → {newSpeed}",
                "scaling" => $"{drive.ParentVehicle.vehicleName}'s speed scaled: {oldSpeed} → {newSpeed}",
                _ => $"{drive.ParentVehicle.vehicleName} speed changed: {oldSpeed} → {newSpeed} ({reason})"
            };
            
            var log = RaceHistory.Log(
                EventType.Movement,
                EventImportance.Low,
                message,
                drive.ParentVehicle.currentStage,
                drive.ParentVehicle
            ).WithMetadata("oldSpeed", oldSpeed)
             .WithMetadata("newSpeed", newSpeed);
            
            if (amount.HasValue)
                log.WithMetadata(reason + "Amount", amount.Value);
        }
        
        /// <summary>
        /// Log speed scaling when maxSpeed changes due to buffs/debuffs.
        /// Includes old/new maxSpeed in metadata.
        /// </summary>
        public static void LogSpeedScaling(this DriveComponent drive, int oldSpeed, int newSpeed, int oldMaxSpeed, int newMaxSpeed)
        {
            if (oldSpeed == newSpeed || drive.ParentVehicle == null) return;
            
            RaceHistory.Log(
                EventType.Modifier,
                EventImportance.Low,
                $"{drive.ParentVehicle.vehicleName}'s speed scaled: {oldSpeed} → {newSpeed} (maxSpeed: {oldMaxSpeed} → {newMaxSpeed})",
                drive.ParentVehicle.currentStage,
                drive.ParentVehicle
            ).WithMetadata("oldSpeed", oldSpeed)
             .WithMetadata("newSpeed", newSpeed)
             .WithMetadata("oldMaxSpeed", oldMaxSpeed)
             .WithMetadata("newMaxSpeed", newMaxSpeed);
        }
        
        /// <summary>
        /// Log target speed setting.
        /// </summary>
        public static void LogTargetSpeedSet(this DriveComponent drive, int oldPercent, int newPercent)
        {
            if (oldPercent == newPercent || drive.ParentVehicle == null) return;
            
            int maxSpeed = drive.GetMaxSpeed();
            int targetAbsolute = (newPercent * maxSpeed) / 100;
            
            RaceHistory.Log(
                EventType.Movement,
                EventImportance.Low,
                $"{drive.ParentVehicle.vehicleName} set target speed: {oldPercent}% → {newPercent}% ({targetAbsolute} units/turn)",
                drive.ParentVehicle.currentStage,
                drive.ParentVehicle
            ).WithMetadata("oldTargetPercent", oldPercent)
             .WithMetadata("newTargetPercent", newPercent)
             .WithMetadata("targetAbsolute", targetAbsolute)
             .WithMetadata("currentSpeed", drive.GetCurrentSpeed())
             .WithMetadata("maxSpeed", maxSpeed);
        }
        
        // ==================== DAMAGE/HEALTH LOGGING ====================
        
        /// <summary>
        /// Log health change for any entity.
        /// Automatically determines if healing or damage based on direction.
        /// </summary>
        public static void LogHealthChange(this Entity entity, int oldHealth, int newHealth, string reason)
        {
            if (oldHealth == newHealth) return;
            
            global::Vehicle vehicle = EntityHelpers.GetParentVehicle(entity);
            string entityName = EntityHelpers.GetEntityDisplayName(entity);
            
            string message = newHealth > oldHealth
                ? $"{entityName} healed: {oldHealth} → {newHealth} HP ({reason})"
                : $"{entityName} damaged: {oldHealth} → {newHealth} HP ({reason})";
            
            RaceHistory.Log(
                EventType.Combat,
                newHealth <= 0 ? EventImportance.High : EventImportance.Low,
                message,
                vehicle?.currentStage,
                vehicle
            ).WithMetadata("oldHealth", oldHealth)
             .WithMetadata("newHealth", newHealth)
             .WithMetadata("reason", reason);
        }
        
        /// <summary>
        /// Log chassis destruction (structural collapse - vehicle destroyed).
        /// </summary>
        public static void LogChassisDestroyed(this ChassisComponent chassis)
        {
            if (chassis.ParentVehicle == null) return;
            
            RaceHistory.Log(
                EventType.Combat,
                EventImportance.Critical,
                $"[CRITICAL] {chassis.ParentVehicle.vehicleName}'s Chassis destroyed! Vehicle structural collapse imminent!",
                chassis.ParentVehicle.currentStage,
                chassis.ParentVehicle
            ).WithMetadata("componentName", chassis.name)
             .WithMetadata("componentType", "Chassis")
             .WithMetadata("catastrophicFailure", true);
        }
        
        /// <summary>
        /// Log power core destruction (energy system failure - vehicle powerless).
        /// </summary>
        public static void LogPowerCoreDestroyed(this PowerCoreComponent powerCore)
        {
            if (powerCore.ParentVehicle == null) return;
            
            RaceHistory.Log(
                EventType.Combat,
                EventImportance.Critical,
                $"[CRITICAL] {powerCore.ParentVehicle.vehicleName}'s Power Core destroyed! Vehicle is powerless!",
                powerCore.ParentVehicle.currentStage,
                powerCore.ParentVehicle
            ).WithMetadata("componentName", powerCore.name)
             .WithMetadata("componentType", "PowerCore")
             .WithMetadata("currentEnergy", 0)
             .WithMetadata("catastrophicFailure", true);
        }
        
        /// <summary>
        /// Log component destruction (generic - for non-critical components).
        /// </summary>
        public static void LogComponentDestroyed(this VehicleComponent component)
        {
            if (component.ParentVehicle == null) return;
            
            RaceHistory.Log(
                EventType.Combat,
                EventImportance.High,
                $"[DESTROYED] {component.ParentVehicle.vehicleName}'s {component.name} was destroyed!",
                component.ParentVehicle.currentStage,
                component.ParentVehicle
            ).WithMetadata("componentName", component.name)
             .WithMetadata("componentType", component.componentType.ToString());
        }
        
        /// <summary>
        /// Log modifier removal from component.
        /// </summary>
        public static void LogModifierRemoved(this VehicleComponent component, AttributeModifier modifier)
        {
            if (component.ParentVehicle == null) return;
            
            RaceHistory.Log(
                EventType.Modifier,
                EventImportance.Debug,
                $"{component.ParentVehicle.vehicleName}'s {component.name} lost {modifier.Type} {modifier.Attribute} {modifier.Value:+0;-0} modifier",
                component.ParentVehicle.currentStage,
                component.ParentVehicle
            ).WithMetadata("component", component.name)
             .WithMetadata("modifierType", modifier.Type.ToString())
             .WithMetadata("attribute", modifier.Attribute.ToString())
             .WithMetadata("removed", true);
        }
        
        /// <summary>
        /// Log component power starvation (insufficient power to operate).
        /// </summary>
        public static void LogPowerStarved(this VehicleComponent component, int requiredPower)
        {
            if (component.ParentVehicle == null) return;
            
            RaceHistory.Log(
                EventType.Resource,
                EventImportance.Medium,
                $"{component.ParentVehicle.vehicleName}: {component.name} shut down due to insufficient power",
                component.ParentVehicle.currentStage,
                component.ParentVehicle
            ).WithMetadata("component", component.name)
             .WithMetadata("requiredPower", requiredPower)
             .WithMetadata("reason", "InsufficientPower");
        }
        
        /// <summary>
        /// Log manual component enable/disable by engineer.
        /// </summary>
        public static void LogManualStateChange(this VehicleComponent component, bool isDisabled)
        {
            if (component.ParentVehicle == null) return;
            
            string state = isDisabled ? "disabled" : "enabled";
            
            RaceHistory.Log(
                EventType.Resource,
                EventImportance.Low,
                $"{component.ParentVehicle.vehicleName}: {component.name} {state} by engineer",
                component.ParentVehicle.currentStage,
                component.ParentVehicle
            ).WithMetadata("component", component.name)
             .WithMetadata("manuallyDisabled", isDisabled);
        }
        
        // ==================== RESOURCE LOGGING ====================
        
        /// <summary>
        /// Log energy change for power core.
        /// Automatically formats as +/- change.
        /// </summary>
        public static void LogEnergyChange(this PowerCoreComponent powerCore, int oldEnergy, int newEnergy, string reason)
        {
            if (oldEnergy == newEnergy || powerCore.ParentVehicle == null) return;
            
            int change = newEnergy - oldEnergy;
            string changeStr = change > 0 ? $"+{change}" : change.ToString();
            
            RaceHistory.Log(
                EventType.Resource,
                EventImportance.Debug,
                $"{powerCore.ParentVehicle.vehicleName} energy {changeStr}: {oldEnergy} → {newEnergy} ({reason})",
                powerCore.ParentVehicle.currentStage,
                powerCore.ParentVehicle
            ).WithMetadata("oldEnergy", oldEnergy)
             .WithMetadata("newEnergy", newEnergy)
             .WithMetadata("change", change)
             .WithMetadata("reason", reason);
        }
        
        /// <summary>
        /// Log energy regeneration at start of turn.
        /// </summary>
        public static void LogEnergyRegeneration(this PowerCoreComponent powerCore, int regenAmount, int currentEnergy, int maxEnergy)
        {
            if (regenAmount <= 0 || powerCore.ParentVehicle == null) return;
            
            RaceHistory.Log(
                EventType.Resource,
                EventImportance.Debug,
                $"{powerCore.ParentVehicle.vehicleName} regenerated {regenAmount} energy ({currentEnergy}/{maxEnergy})",
                powerCore.ParentVehicle.currentStage,
                powerCore.ParentVehicle
            ).WithMetadata("regenAmount", regenAmount)
             .WithMetadata("currentEnergy", currentEnergy)
             .WithMetadata("maxEnergy", maxEnergy);
        }
    }
}
