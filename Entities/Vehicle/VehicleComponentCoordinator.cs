using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts.Entities.Vehicle.VehicleComponents.ComponentTypes;

namespace Assets.Scripts.Entities.Vehicle
{
    /// <summary>Component discovery, initialization, accessibility, and cross-component modifiers.</summary>
    public class VehicleComponentCoordinator
    {
        private readonly global::Vehicle vehicle;

        public VehicleComponentCoordinator(global::Vehicle vehicle)
        {
            this.vehicle = vehicle;
        }

        // ==================== COMPONENT INITIALIZATION ====================

        /// <summary>
        /// Components are discovered in Unity hierarchy automatically.
        /// </summary>
        public void InitializeComponents()
        {
            var allFoundComponents = vehicle.GetComponentsInChildren<VehicleComponent>();

            foreach (var component in allFoundComponents)
            {
                component.Initialize(vehicle);

                if (component is ChassisComponent && vehicle.chassis == null)
                {
                    vehicle.chassis = component as ChassisComponent;
                }
                else if (component is PowerCoreComponent && vehicle.powerCore == null)
                {
                    vehicle.powerCore = component as PowerCoreComponent;
                }
                else
                {
                    if (!vehicle.optionalComponents.Contains(component))
                    {
                        vehicle.optionalComponents.Add(component);
                    }
                }
            }

            // Chassis and PowerCore are mandatory - the vehicle doesn't function without them.
            if (vehicle.chassis == null)
                Debug.LogWarning($"[Vehicle] {vehicle.vehicleName} has no Chassis component! Vehicle stats will be incomplete.");

            if (vehicle.powerCore == null)
                Debug.LogWarning($"[Vehicle] {vehicle.vehicleName} has no Power Core component! Vehicle will have no power.");

            // Cross-component modifiers depend on all components being discovered first
            InitializeComponentModifiers();

            Debug.Log($"[Vehicle] {vehicle.vehicleName} initialized with {GetAllComponents().Count} component(s)");
        }

        // ==================== COMPONENT ACCESSIBILITY ====================

        public bool IsComponentAccessible(VehicleComponent target)
        {
            if (target == null || target.isDestroyed)
                return false;

            if (target.exposure == ComponentExposure.External)
                return true;

            // Protected/Shielded components - inaccessible until their shield is destroyed.
            if ((target.exposure == ComponentExposure.Protected || target.exposure == ComponentExposure.Shielded)
                && target.shieldedByComponent != null)
            {
                return target.shieldedByComponent.isDestroyed;
            }

            // Internal components - inaccessible until chassis is damaged enough.
            if (target.exposure == ComponentExposure.Internal)
            {
                if (vehicle.chassis == null) return true; // Fallback if no chassis

                int chassisDamagePercent = 100 - (vehicle.chassis.GetCurrentHealth() * 100 / vehicle.chassis.GetMaxHealth());
                return chassisDamagePercent >= target.internalAccessThreshold;
            }

            return true;
        }

        /// <summary>Null if accessible.</summary>
        public string GetInaccessibilityReason(VehicleComponent target)
        {
            if (target == null || target.isDestroyed)
                return "Component destroyed";

            if (IsComponentAccessible(target))
                return null;

            if ((target.exposure == ComponentExposure.Protected || target.exposure == ComponentExposure.Shielded)
                && target.shieldedByComponent != null)
            {
                if (!target.shieldedByComponent.isDestroyed)
                    return $"Shielded by {target.shieldedByComponent.name}";
            }

            if (target.exposure == ComponentExposure.Internal)
            {
                if (vehicle.chassis != null)
                {
                    int chassisDamagePercent = 100 - (vehicle.chassis.GetCurrentHealth() * 100 / vehicle.chassis.GetMaxHealth());
                    if (chassisDamagePercent < target.internalAccessThreshold)
                    {
                        return $"Chassis must be {target.internalAccessThreshold}% damaged";
                    }
                }
            }

            return "Cannot target";
        }

        // ==================== COMPONENT QUERIES ====================

        public List<VehicleComponent> GetAllComponents()
        {
            List<VehicleComponent> all = new();
            if (vehicle.chassis != null) all.Add(vehicle.chassis);
            if (vehicle.powerCore != null) all.Add(vehicle.powerCore);
            if (vehicle.optionalComponents != null) all.AddRange(vehicle.optionalComponents);
            return all;
        }

        // ==================== COMPONENT SPACE VALIDATION ====================

        /// <summary>Positive = over capacity.</summary>
        public int CalculateNetComponentSpace()
        {
            int total = 0;
            foreach (var component in GetAllComponents())
            {
                if (component != null)
                {
                    total += component.GetComponentSpace();
                }
            }
            return total;
        }

        // ==================== CROSS-COMPONENT MODIFIER SYSTEM ====================

        /// <summary>Apply cross-component modifiers (e.g. Armor Plating → Chassis +2 AC). Called once at init.</summary>
        public void InitializeComponentModifiers()
        {
            foreach (var provider in GetAllComponents())
            {
                if (provider.IsOperational)
                {
                    provider.ApplyProvidedModifiers(vehicle);
                }
            }
        }

        public void ApplySizeModifiers(VehicleEffectRouter effectRouter)
        {
            if (vehicle.chassis == null)
            {
                Debug.LogWarning($"[Vehicle] {vehicle.vehicleName} has no chassis - cannot apply size modifiers");
                return;
            }

            var sizeModifiers = VehicleSizeModifiers.GetModifiers(vehicle.chassis.sizeCategory, vehicle);

            foreach (var modifier in sizeModifiers)
            {
                VehicleComponent targetComponent = effectRouter.ResolveModifierTarget(modifier.Attribute);

                if (targetComponent != null && !targetComponent.isDestroyed)
                {
                    targetComponent.AddModifier(modifier);
                }
            }
        }
    }
}
