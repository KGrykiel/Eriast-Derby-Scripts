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
                "friction"     => $"{LogColors.Vehicle(drive.ParentVehicle.vehicleName)} slowed by friction: {LogColors.Number($"{oldSpeed} -> {newSpeed}")}",
                "acceleration" => $"{LogColors.Vehicle(drive.ParentVehicle.vehicleName)} accelerated: {LogColors.Number($"{oldSpeed} -> {newSpeed}")}",
                "deceleration" => $"{LogColors.Vehicle(drive.ParentVehicle.vehicleName)} decelerated: {LogColors.Number($"{oldSpeed} -> {newSpeed}")}",
                "scaling"      => $"{LogColors.Vehicle(drive.ParentVehicle.vehicleName)}'s speed scaled: {LogColors.Number($"{oldSpeed} -> {newSpeed}")}",
                _              => $"{LogColors.Vehicle(drive.ParentVehicle.vehicleName)} speed changed: {LogColors.Number($"{oldSpeed} -> {newSpeed}")} ({reason})"
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
                $"{LogColors.Vehicle(drive.ParentVehicle.vehicleName)}'s speed scaled: {LogColors.Number($"{oldSpeed} -> {newSpeed}")} (maxSpeed: {LogColors.Number($"{oldMaxSpeed} -> {newMaxSpeed}")})",
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
                $"{LogColors.Vehicle(drive.ParentVehicle.vehicleName)} set target speed: {LogColors.Number($"{oldPercent}%")} -> {LogColors.Number($"{newPercent}%")} ({LogColors.Number($"{targetAbsolute} units/turn")})",
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
                $"{LogColors.Vehicle(component.ParentVehicle.vehicleName)}'s {LogColors.Component(component.name)} lost {modifier.Type} {modifier.Attribute} {LogColors.Number($"{modifier.Value:+0;-0}")} modifier",
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
                $"{LogColors.Vehicle(powerCore.ParentVehicle.vehicleName)} regenerated {LogColors.Energy($"{regenAmount} energy")} ({LogColors.Number($"{currentEnergy}/{maxEnergy}")})",
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
                $"{LogColors.Vehicle(powerCore.ParentVehicle.vehicleName)}: {requesterName} drew {LogColors.Energy($"{amount} energy")} ({reason})",
                RacePositionTracker.GetStage(powerCore.ParentVehicle),
                powerCore.ParentVehicle
            );
        }
    }
}

