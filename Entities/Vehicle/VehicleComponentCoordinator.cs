using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts.Entities.Vehicle.VehicleComponents.ComponentTypes;

namespace Assets.Scripts.Entities.Vehicle
{
    /// <summary>
    /// Manages vehicle component discovery, initialization, role management, and accessibility.
    /// Separates component coordination logic from Vehicle MonoBehaviour.
    /// </summary>
    public class VehicleComponentCoordinator
    {
        private readonly global::Vehicle vehicle;

        public VehicleComponentCoordinator(global::Vehicle vehicle)
        {
            this.vehicle = vehicle;
        }

        // ==================== COMPONENT INITIALIZATION ====================

        /// <summary>
        /// Discover and initialize all vehicle components.
        /// Called automatically in Vehicle.Awake().
        /// </summary>
        public void InitializeComponents()
        {
            // Find all VehicleComponent child objects
            var allFoundComponents = vehicle.GetComponentsInChildren<VehicleComponent>();

            // Auto-categorize components
            foreach (var component in allFoundComponents)
            {
                // Initialize component with vehicle reference
                component.Initialize(vehicle);

                // Auto-assign mandatory components if not manually set
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
                    // Add to optional components if not already there
                    if (!vehicle.optionalComponents.Contains(component))
                    {
                        vehicle.optionalComponents.Add(component);
                    }
                }
            }

            // Validate mandatory components
            if (vehicle.chassis == null)
            {
                Debug.LogWarning($"[Vehicle] {vehicle.vehicleName} has no Chassis component! Vehicle stats will be incomplete.");
            }

            if (vehicle.powerCore == null)
            {
                Debug.LogWarning($"[Vehicle] {vehicle.vehicleName} has no Power Core component! Vehicle will have no power.");
            }

            // Apply cross-component modifiers after all components are discovered
            InitializeComponentModifiers();

            // Log component discovery
            Debug.Log($"[Vehicle] {vehicle.vehicleName} initialized with {GetAllComponents().Count} component(s)");
        }

        // ==================== COMPONENT ACCESSIBILITY ====================

        /// <summary>
        /// Check if a component is currently accessible for targeting.
        /// External components always accessible.
        /// Protected/Shielded components require shield destruction.
        /// Internal components require chassis damage (threshold set per component).
        /// </summary>
        public bool IsComponentAccessible(VehicleComponent target)
        {
            if (target == null || target.isDestroyed)
                return false;

            // External components are always accessible
            if (target.exposure == ComponentExposure.External)
                return true;

            // Protected/Shielded components: check if shielding component is destroyed
            if ((target.exposure == ComponentExposure.Protected || target.exposure == ComponentExposure.Shielded)
                && target.shieldedByComponent != null)
            {
                // Accessible if shield is destroyed
                return target.shieldedByComponent.isDestroyed;
            }

            // Internal components: requires chassis damage based on component's threshold
            if (target.exposure == ComponentExposure.Internal)
            {
                if (vehicle.chassis == null) return true; // Fallback if no chassis

                // Calculate chassis damage percentage (1.0 = fully damaged, 0.0 = undamaged)
                float chassisDamagePercent = 1f - ((float)vehicle.chassis.health / (float)vehicle.chassis.GetMaxHealth());

                // Accessible if chassis damage >= threshold
                return chassisDamagePercent >= target.internalAccessThreshold;
            }

            // Default: accessible
            return true;
        }

        /// <summary>
        /// Get the reason why a component cannot be accessed (for UI display).
        /// Returns null if component is accessible.
        /// </summary>
        public string GetInaccessibilityReason(VehicleComponent target)
        {
            if (target == null || target.isDestroyed)
                return "Component destroyed";

            if (IsComponentAccessible(target))
                return null; // Accessible

            // Protected/Shielded components
            if ((target.exposure == ComponentExposure.Protected || target.exposure == ComponentExposure.Shielded)
                && target.shieldedByComponent != null)
            {
                if (!target.shieldedByComponent.isDestroyed)
                    return $"Shielded by {target.shieldedByComponent.name}";
            }

            // Internal components
            if (target.exposure == ComponentExposure.Internal)
            {
                if (vehicle.chassis != null)
                {
                    float chassisDamagePercent = 1f - ((float)vehicle.chassis.health / (float)vehicle.chassis.GetMaxHealth());
                    if (chassisDamagePercent < target.internalAccessThreshold)
                    {
                        int requiredDamagePercent = Mathf.RoundToInt(target.internalAccessThreshold * 100f);
                        return $"Chassis must be {requiredDamagePercent}% damaged";
                    }
                }
            }

            return "Cannot target";
        }

        // ==================== COMPONENT QUERIES ====================

        /// <summary>
        /// Get all components (mandatory + optional).
        /// </summary>
        public List<VehicleComponent> GetAllComponents()
        {
            List<VehicleComponent> all = new();
            if (vehicle.chassis != null) all.Add(vehicle.chassis);
            if (vehicle.powerCore != null) all.Add(vehicle.powerCore);
            if (vehicle.optionalComponents != null) all.AddRange(vehicle.optionalComponents);
            return all;
        }

        // ==================== COMPONENT SPACE VALIDATION ====================

        /// <summary>
        /// Calculate total component space used.
        /// Returns positive value if over capacity (exceeds chassis space).
        /// </summary>
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

        /// <summary>
        /// Initialize all component-provided modifiers.
        /// Called once during vehicle initialization after all components are discovered.
        /// Components provide modifiers to OTHER components (e.g., Armor Plating → Chassis +2 AC).
        /// For runtime changes (destroy/disable), components handle their own modifier cleanup.
        /// </summary>
        public void InitializeComponentModifiers()
        {
            // Apply modifiers from all active (non-destroyed, non-disabled) providers
            foreach (var provider in GetAllComponents())
            {
                if (provider.IsOperational)
                {
                    provider.ApplyProvidedModifiers(vehicle);
                }
            }
        }

        /// <summary>
        /// Apply size-based modifiers to all components.
        /// Called during vehicle initialization.
        /// Size modifiers automatically route to correct components (AC→Chassis, Speed→Drive, etc.).
        /// Requires VehicleEffectRouter for attribute-to-component resolution.
        /// </summary>
        public void ApplySizeModifiers(VehicleEffectRouter effectRouter)
        {
            if (vehicle.chassis == null)
            {
                Debug.LogWarning($"[Vehicle] {vehicle.vehicleName} has no chassis - cannot apply size modifiers");
                return;
            }

            // Get size modifiers from chassis's size category
            var sizeModifiers = VehicleSizeModifiers.GetModifiers(vehicle.chassis.sizeCategory, vehicle);

            // Apply each modifier to the appropriate component
            foreach (var modifier in sizeModifiers)
            {
                // Route modifier to correct component based on attribute
                VehicleComponent targetComponent = effectRouter.ResolveModifierTarget(modifier.Attribute);

                if (targetComponent != null && !targetComponent.isDestroyed)
                {
                    targetComponent.AddModifier(modifier);
                }
            }
        }
    }
}
