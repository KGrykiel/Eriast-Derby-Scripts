using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts.Entities.Vehicles.VehicleComponents.ComponentTypes;
using Assets.Scripts.Entities.Vehicles.VehicleComponents;

namespace Assets.Scripts.Entities.Vehicles
{
    /// <summary>Component discovery, initialization, accessibility, and cross-component modifiers.</summary>
    public class VehicleComponentCoordinator
    {
        private readonly Vehicle vehicle;
        private readonly List<VehicleComponent> _components = new();

        public ChassisComponent Chassis { get; private set; }
        public PowerCoreComponent PowerCore { get; private set; }

        public VehicleComponentCoordinator(Vehicle vehicle)
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
                RegisterComponent(component);
            }

            // Chassis and PowerCore are mandatory - the vehicle doesn't function without them.
            if (Chassis == null)
                Debug.LogWarning($"[Vehicle] {vehicle.vehicleName} has no Chassis component! Vehicle stats will be incomplete.");

            if (PowerCore == null)
                Debug.LogWarning($"[Vehicle] {vehicle.vehicleName} has no Power Core component! Vehicle will have no power.");

            // Cross-component modifiers depend on all components being discovered first
            InitializeComponentModifiers();

            Debug.Log($"[Vehicle] {vehicle.vehicleName} initialized with {_components.Count} component(s)");
        }

        public void RegisterComponent(VehicleComponent component)
        {
            if (component == null || _components.Contains(component)) return;
            _components.Add(component);
            if (component is ChassisComponent chassis && Chassis == null)
                Chassis = chassis;
            else if (component is PowerCoreComponent powerCore && PowerCore == null)
                PowerCore = powerCore;
        }

        // ==================== COMPONENT ACCESSIBILITY ====================

        public bool IsComponentAccessible(VehicleComponent target)
        {
            if (target == null || target.IsDestroyed())
                return false;

            if (target.exposure == ComponentExposure.External)
                return true;

            // Protected/Shielded components - inaccessible until their shield is destroyed.
            if ((target.exposure == ComponentExposure.Protected || target.exposure == ComponentExposure.Shielded)
                && target.shieldedByComponent != null)
            {
                return target.shieldedByComponent.IsDestroyed();
            }

            // Internal components - inaccessible until chassis is damaged enough.
            if (target.exposure == ComponentExposure.Internal)
            {
                if (Chassis == null) return true; // Fallback if no chassis

                int chassisDamagePercent = 100 - (Chassis.GetCurrentHealth() * 100 / Chassis.GetMaxHealth());
                return chassisDamagePercent >= target.internalAccessThreshold;
            }

            return true;
        }

        /// <summary>Null if accessible.</summary>
        public string GetInaccessibilityReason(VehicleComponent target)
        {
            if (target == null || target.IsDestroyed())
                return "Component destroyed";

            if (IsComponentAccessible(target))
                return null;

            if ((target.exposure == ComponentExposure.Protected || target.exposure == ComponentExposure.Shielded)
                && target.shieldedByComponent != null)
            {
                if (!target.shieldedByComponent.IsDestroyed())
                    return $"Shielded by {target.shieldedByComponent.name}";
            }

            if (target.exposure == ComponentExposure.Internal)
            {
                if (Chassis != null)
                {
                    int chassisDamagePercent = 100 - (Chassis.GetCurrentHealth() * 100 / Chassis.GetMaxHealth());
                    if (chassisDamagePercent < target.internalAccessThreshold)
                    {
                        return $"Chassis must be {target.internalAccessThreshold}% damaged";
                    }
                }
            }

            return "Cannot target";
        }

        // ==================== COMPONENT QUERIES ====================

        public IReadOnlyList<VehicleComponent> GetAllComponents() => _components;

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

        /// <summary>Apply cross-component modifiers (e.g. Armor Plating -> Chassis +2 AC). Called once at init.</summary>
        public void InitializeComponentModifiers()
        {
            foreach (var provider in GetAllComponents())
            {
                if (provider.IsOperational)
                {
                    provider.ApplyProvidedModifiers(vehicle);
                    provider.ApplyProvidedAdvantageGrants(vehicle);
                }
            }
        }

        public void ApplySizeModifiers()
        {
            if (Chassis == null)
            {
                Debug.LogWarning($"[Vehicle] {vehicle.vehicleName} has no chassis - cannot apply size modifiers");
                return;
            }

            var sizeModifiers = VehicleSizeModifiers.GetModifiers(Chassis.sizeCategory);

            foreach (var modifier in sizeModifiers)
            {
                VehicleComponent targetComponent = VehicleComponentResolver.ResolveForAttribute(vehicle, modifier.Attribute);

                if (targetComponent != null && !targetComponent.IsDestroyed())
                {
                    modifier.Source = targetComponent;
                    targetComponent.AddModifier(modifier);
                }
            }
        }
    }
}
