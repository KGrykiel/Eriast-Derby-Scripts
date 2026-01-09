using System.Linq;
using UnityEngine;
using Assets.Scripts.Entities.Vehicle.VehicleComponents.ComponentTypes;

namespace Assets.Scripts.Entities.Vehicle
{
    /// <summary>
    /// Convenience properties for Vehicle that delegate to components.
    /// Provides a clean API for accessing vehicle stats without direct component access.
    /// All properties apply modifiers automatically through component methods.
    /// </summary>
    public static class VehicleProperties
    {
        // ==================== HEALTH (Chassis) ====================

        /// <summary>
        /// Get vehicle health (delegates to chassis).
        /// </summary>
        public static int GetHealth(global::Vehicle vehicle)
        {
            if (vehicle.chassis == null)
                return 0;
            return vehicle.chassis.health;
        }

        /// <summary>
        /// Set vehicle health (delegates to chassis).
        /// Clamped between 0 and maxHealth.
        /// </summary>
        public static void SetHealth(global::Vehicle vehicle, int value)
        {
            if (vehicle.chassis == null)
                return;

            // Clamp to valid range (use GetMaxHP for modifier-adjusted max)
            vehicle.chassis.health = Mathf.Clamp(value, 0, vehicle.chassis.GetMaxHP());

            // Check for destruction
            if (vehicle.chassis.health <= 0 && !vehicle.chassis.isDestroyed)
            {
                vehicle.chassis.isDestroyed = true;
                vehicle.DestroyVehicle();
            }
        }

        /// <summary>
        /// Get maximum health (chassis base HP + bonuses + modifiers).
        /// </summary>
        public static int GetMaxHealth(global::Vehicle vehicle)
        {
            if (vehicle.chassis == null) return 0;
            return vehicle.chassis.GetMaxHP();
        }

        // ==================== ENERGY (Power Core) ====================

        /// <summary>
        /// Get current energy (stored in power core).
        /// </summary>
        public static int GetEnergy(global::Vehicle vehicle)
        {
            if (vehicle.powerCore == null) return 0;
            return vehicle.powerCore.currentEnergy;
        }

        /// <summary>
        /// Set current energy (stored in power core).
        /// Clamped between 0 and maxEnergy.
        /// </summary>
        public static void SetEnergy(global::Vehicle vehicle, int value)
        {
            if (vehicle.powerCore == null) return;
            vehicle.powerCore.currentEnergy = Mathf.Clamp(value, 0, vehicle.powerCore.GetMaxEnergy());
        }

        /// <summary>
        /// Get maximum energy capacity (with modifiers applied).
        /// </summary>
        public static int GetMaxEnergy(global::Vehicle vehicle)
        {
            if (vehicle.powerCore == null) return 0;
            return vehicle.powerCore.GetMaxEnergy();
        }

        /// <summary>
        /// Get energy regeneration rate (with modifiers applied).
        /// </summary>
        public static float GetEnergyRegen(global::Vehicle vehicle)
        {
            if (vehicle.powerCore == null) return 0f;
            return vehicle.powerCore.GetEnergyRegen();
        }

        // ==================== SPEED (Drive) ====================

        /// <summary>
        /// Get vehicle speed (from drive component with modifiers applied).
        /// </summary>
        public static float GetSpeed(global::Vehicle vehicle)
        {
            var drive = vehicle.optionalComponents.OfType<DriveComponent>().FirstOrDefault();
            if (drive != null && !drive.isDestroyed && !drive.isDisabled)
            {
                return drive.GetSpeed();
            }
            return 0f;
        }

        // ==================== ARMOR CLASS (Chassis) ====================

        /// <summary>
        /// Get vehicle armor class (chassis base AC + bonuses + modifiers).
        /// </summary>
        public static int GetArmorClass(global::Vehicle vehicle)
        {
            if (vehicle.chassis == null) return 10; // Default AC
            return vehicle.chassis.GetTotalAC();
        }

        /// <summary>
        /// Get AC for targeting a specific component.
        /// Returns modifier-adjusted AC if component is a ChassisComponent.
        /// </summary>
        public static int GetComponentAC(global::Vehicle vehicle, VehicleComponent targetComponent)
        {
            if (targetComponent == null)
                return GetArmorClass(vehicle); // Fallback to chassis AC

            // If targeting chassis, use modifier-adjusted AC
            if (targetComponent is ChassisComponent chassis)
            {
                return chassis.GetTotalAC();
            }

            // For other components, use base AC (they don't have GetTotalAC yet)
            // TODO: Add GetTotalAC() to all VehicleComponents for modifier support
            return targetComponent.armorClass;
        }
    }
}
