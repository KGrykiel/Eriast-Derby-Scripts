using Assets.Scripts.Logging;
using Assets.Scripts.Entities.Vehicles.VehicleComponents.ComponentTypes;
using EventType = Assets.Scripts.Logging.EventType;
using Assets.Scripts.Entities.Vehicles.VehicleComponents;
using Assets.Scripts.Modifiers;

namespace Assets.Scripts.Entities
{
    public static class EntityLogManager
    {
        // ==================== MOVEMENT LOGGING ====================

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
            
            RaceHistory.Log(
                EventType.Movement,
                EventImportance.Low,
                message,
                drive.ParentVehicle.CurrentStage,
                drive.ParentVehicle
            );
        }
        
        public static void LogSpeedScaling(this DriveComponent drive, int oldSpeed, int newSpeed, int oldMaxSpeed, int newMaxSpeed)
        {
            if (oldSpeed == newSpeed || drive.ParentVehicle == null) return;
            
            RaceHistory.Log(
                EventType.Modifier,
                EventImportance.Low,
                $"{drive.ParentVehicle.vehicleName}'s speed scaled: {oldSpeed} → {newSpeed} (maxSpeed: {oldMaxSpeed} → {newMaxSpeed})",
                drive.ParentVehicle.CurrentStage,
                drive.ParentVehicle
            );
        }
        
        public static void LogTargetSpeedSet(this DriveComponent drive, int oldPercent, int newPercent)
        {
            if (oldPercent == newPercent || drive.ParentVehicle == null) return;
            
            int maxSpeed = drive.GetMaxSpeed();
            int targetAbsolute = (newPercent * maxSpeed) / 100;
            
            RaceHistory.Log(
                EventType.Movement,
                EventImportance.Low,
                $"{drive.ParentVehicle.vehicleName} set target speed: {oldPercent}% → {newPercent}% ({targetAbsolute} units/turn)",
                drive.ParentVehicle.CurrentStage,
                drive.ParentVehicle
            );
        }
        
        // ==================== DAMAGE/HEALTH LOGGING ====================
        
        public static void LogChassisDestroyed(this ChassisComponent chassis)
        {
            if (chassis.ParentVehicle == null) return;
            
            RaceHistory.Log(
                EventType.Combat,
                EventImportance.Critical,
                $"[CRITICAL] {chassis.ParentVehicle.vehicleName}'s Chassis destroyed! Vehicle structural collapse imminent!",
                chassis.ParentVehicle.CurrentStage,
                chassis.ParentVehicle
            );
        }
        
        public static void LogPowerCoreDestroyed(this PowerCoreComponent powerCore)
        {
            if (powerCore.ParentVehicle == null) return;
            
            RaceHistory.Log(
                EventType.Combat,
                EventImportance.Critical,
                $"[CRITICAL] {powerCore.ParentVehicle.vehicleName}'s Power Core destroyed! Vehicle is powerless!",
                powerCore.ParentVehicle.CurrentStage,
                powerCore.ParentVehicle
            );
        }
        
        public static void LogComponentDestroyed(this VehicleComponent component)
        {
            if (component.ParentVehicle == null) return;
            
            RaceHistory.Log(
                EventType.Combat,
                EventImportance.High,
                $"[DESTROYED] {component.ParentVehicle.vehicleName}'s {component.name} was destroyed!",
                component.ParentVehicle.CurrentStage,
                component.ParentVehicle
            );
        }
        
        public static void LogModifierRemoved(this VehicleComponent component, EntityAttributeModifier modifier)
        {
            if (component.ParentVehicle == null) return;

            RaceHistory.Log(
                EventType.Modifier,
                EventImportance.Debug,
                $"{component.ParentVehicle.vehicleName}'s {component.name} lost {modifier.Type} {modifier.Attribute} {modifier.Value:+0;-0} modifier",
                component.ParentVehicle.CurrentStage,
                component.ParentVehicle
            );
        }
        
        public static void LogPowerStarved(this VehicleComponent component, int requiredPower)
        {
            if (component.ParentVehicle == null) return;
            
            RaceHistory.Log(
                EventType.Resource,
                EventImportance.Medium,
                $"{component.ParentVehicle.vehicleName}: {component.name} shut down due to insufficient power",
                component.ParentVehicle.CurrentStage,
                component.ParentVehicle
            );
        }
        
        public static void LogManualStateChange(this VehicleComponent component, bool isDisabled)
        {
            if (component.ParentVehicle == null) return;
            
            string state = isDisabled ? "disabled" : "enabled";
            
            RaceHistory.Log(
                EventType.Resource,
                EventImportance.Low,
                $"{component.ParentVehicle.vehicleName}: {component.name} {state} by engineer",
                component.ParentVehicle.CurrentStage,
                component.ParentVehicle
            );
        }

        // ==================== RESOURCE LOGGING ====================
        
        public static void LogEnergyRegeneration(this PowerCoreComponent powerCore, int regenAmount, int currentEnergy, int maxEnergy)
        {
            if (regenAmount <= 0 || powerCore.ParentVehicle == null) return;
            
            RaceHistory.Log(
                EventType.Resource,
                EventImportance.Debug,
                $"{powerCore.ParentVehicle.vehicleName} regenerated {regenAmount} energy ({currentEnergy}/{maxEnergy})",
                powerCore.ParentVehicle.CurrentStage,
                powerCore.ParentVehicle
            );
        }
        
        public static void LogPowerDraw(this PowerCoreComponent powerCore, int amount, VehicleComponent requester, string reason, int remainingEnergy, int turnDrawTotal)
        {
            if (powerCore.ParentVehicle == null) return;

            string requesterName = requester != null ? requester.name : "Unknown";

            RaceHistory.Log(
                EventType.Resource,
                EventImportance.Debug,
                $"{powerCore.ParentVehicle.vehicleName}: {requesterName} drew {amount} power ({reason})",
                powerCore.ParentVehicle.CurrentStage,
                powerCore.ParentVehicle
            );
        }
    }
}
