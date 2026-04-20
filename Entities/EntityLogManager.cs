using Assets.Scripts.Logging;
using Assets.Scripts.Managers;
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
                "friction"     => $"{LogColors.Vehicle(drive.ParentVehicle.vehicleName)} slowed by friction: <color={LogColors.Number}>{oldSpeed} -> {newSpeed}</color>",
                "acceleration" => $"{LogColors.Vehicle(drive.ParentVehicle.vehicleName)} accelerated: <color={LogColors.Number}>{oldSpeed} -> {newSpeed}</color>",
                "deceleration" => $"{LogColors.Vehicle(drive.ParentVehicle.vehicleName)} decelerated: <color={LogColors.Number}>{oldSpeed} -> {newSpeed}</color>",
                "scaling"      => $"{LogColors.Vehicle(drive.ParentVehicle.vehicleName)}'s speed scaled: <color={LogColors.Number}>{oldSpeed} -> {newSpeed}</color>",
                _              => $"{LogColors.Vehicle(drive.ParentVehicle.vehicleName)} speed changed: <color={LogColors.Number}>{oldSpeed} -> {newSpeed}</color> ({reason})"
            };
            
            RaceHistory.Log(
                EventType.Movement,
                EventImportance.Low,
                message,
                RacePositionTracker.GetStage(drive.ParentVehicle),
                drive.ParentVehicle
            );
        }
        
        public static void LogSpeedScaling(this DriveComponent drive, int oldSpeed, int newSpeed, int oldMaxSpeed, int newMaxSpeed)
        {
            if (oldSpeed == newSpeed || drive.ParentVehicle == null) return;
            
            RaceHistory.Log(
                EventType.Modifier,
                EventImportance.Low,
                $"{LogColors.Vehicle(drive.ParentVehicle.vehicleName)}'s speed scaled: <color={LogColors.Number}>{oldSpeed} -> {newSpeed}</color> (maxSpeed: <color={LogColors.Number}>{oldMaxSpeed} -> {newMaxSpeed}</color>)",
                RacePositionTracker.GetStage(drive.ParentVehicle),
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
                $"{LogColors.Vehicle(drive.ParentVehicle.vehicleName)} set target speed: <color={LogColors.Number}>{oldPercent}%</color> -> <color={LogColors.Number}>{newPercent}%</color> (<color={LogColors.Number}>{targetAbsolute} units/turn</color>)",
                RacePositionTracker.GetStage(drive.ParentVehicle),
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
                $"[CRITICAL] {LogColors.Vehicle(chassis.ParentVehicle.vehicleName)}'s Chassis destroyed! Vehicle structural collapse imminent!",
                RacePositionTracker.GetStage(chassis.ParentVehicle),
                chassis.ParentVehicle
            );
        }
        
        public static void LogPowerCoreDestroyed(this PowerCoreComponent powerCore)
        {
            if (powerCore.ParentVehicle == null) return;
            
            RaceHistory.Log(
                EventType.Combat,
                EventImportance.Critical,
                $"[CRITICAL] {LogColors.Vehicle(powerCore.ParentVehicle.vehicleName)}'s Power Core destroyed! Vehicle is powerless!",
                RacePositionTracker.GetStage(powerCore.ParentVehicle),
                powerCore.ParentVehicle
            );
        }
        
        public static void LogComponentDestroyed(this VehicleComponent component)
        {
            if (component.ParentVehicle == null) return;
            
            RaceHistory.Log(
                EventType.Combat,
                EventImportance.High,
                $"[DESTROYED] {LogColors.Vehicle(component.ParentVehicle.vehicleName)}'s {LogColors.Component(component.name)} was destroyed!",
                RacePositionTracker.GetStage(component.ParentVehicle),
                component.ParentVehicle
            );
        }
        
        public static void LogModifierRemoved(this VehicleComponent component, EntityAttributeModifier modifier)
        {
            if (component.ParentVehicle == null) return;

            RaceHistory.Log(
                EventType.Modifier,
                EventImportance.Debug,
                $"{LogColors.Vehicle(component.ParentVehicle.vehicleName)}'s {LogColors.Component(component.name)} lost {modifier.Type} {modifier.Attribute} <color={LogColors.Number}>{modifier.Value:+0;-0}</color> modifier",
                RacePositionTracker.GetStage(component.ParentVehicle),
                component.ParentVehicle
            );
        }
        
        public static void LogPowerStarved(this VehicleComponent component, int requiredPower)
        {
            if (component.ParentVehicle == null) return;
            
            RaceHistory.Log(
                EventType.Resource,
                EventImportance.Medium,
                $"{LogColors.Vehicle(component.ParentVehicle.vehicleName)}: {LogColors.Component(component.name)} shut down due to insufficient energy",
                RacePositionTracker.GetStage(component.ParentVehicle),
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
                $"{LogColors.Vehicle(component.ParentVehicle.vehicleName)}: {LogColors.Component(component.name)} {state} by engineer",
                RacePositionTracker.GetStage(component.ParentVehicle),
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
                $"{LogColors.Vehicle(powerCore.ParentVehicle.vehicleName)} regenerated <color={LogColors.Energy}>{regenAmount} energy</color> (<color={LogColors.Number}>{currentEnergy}/{maxEnergy}</color>)",
                RacePositionTracker.GetStage(powerCore.ParentVehicle),
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
                $"{LogColors.Vehicle(powerCore.ParentVehicle.vehicleName)}: {requesterName} drew <color={LogColors.Energy}>{amount} energy</color> ({reason})",
                RacePositionTracker.GetStage(powerCore.ParentVehicle),
                powerCore.ParentVehicle
            );
        }
    }
}

