using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Assets.Scripts.Entities.Vehicle.VehicleComponents;
using Assets.Scripts.Entities.Vehicle.VehicleComponents.ComponentTypes;
using Assets.Scripts.Entities.Vehicle.VehicleComponents.Enums;

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

            // Log component discovery
            Debug.Log($"[Vehicle] {vehicle.vehicleName} initialized with {GetAllComponents().Count} component(s)");
        }

        // ==================== ROLE DISCOVERY ====================

        /// <summary>
        /// Get all available roles on this vehicle (emergent from components).
        /// Roles are discovered dynamically based on which components enable them.
        /// Returns one VehicleRole struct per component (even if role names are the same).
        /// Example: Two weapons = two separate Gunner roles with different skills/characters.
        /// </summary>
        public List<VehicleRole> GetAvailableRoles()
        {
            List<VehicleRole> roles = new List<VehicleRole>();

            foreach (var component in GetAllComponents())
            {
                // Skip if component doesn't enable a role or is destroyed
                if (!component.enablesRole || component.isDestroyed)
                    continue;

                roles.Add(new VehicleRole
                {
                    roleName = component.roleName,
                    sourceComponent = component,
                    assignedCharacter = component.assignedCharacter,
                    availableSkills = component.GetAllSkills()
                });
            }

            return roles;
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
                float chassisDamagePercent = 1f - ((float)vehicle.chassis.health / (float)vehicle.chassis.maxHealth);

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
                    float chassisDamagePercent = 1f - ((float)vehicle.chassis.health / (float)vehicle.chassis.maxHealth);
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
            List<VehicleComponent> all = new List<VehicleComponent>();
            if (vehicle.chassis != null) all.Add(vehicle.chassis);
            if (vehicle.powerCore != null) all.Add(vehicle.powerCore);
            if (vehicle.optionalComponents != null) all.AddRange(vehicle.optionalComponents);
            return all;
        }

        /// <summary>
        /// Get aggregated stat from all components.
        /// Example: GetComponentStat("HP") returns total HP from all components.
        /// Used internally by Vehicle.GetAttribute() to add component bonuses.
        /// </summary>
        public float GetComponentStat(string statName)
        {
            float total = 0f;

            foreach (var component in GetAllComponents())
            {
                var modifiers = component.GetStatModifiers();
                total += modifiers.GetStat(statName);
            }

            return total;
        }

        // ==================== TURN MANAGEMENT ====================

        /// <summary>
        /// Reset all components for new turn.
        /// Call at start of each round.
        /// </summary>
        public void ResetComponentsForNewTurn()
        {
            foreach (var component in GetAllComponents())
            {
                component.ResetTurnState();
            }
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
                    total += component.componentSpace;
                }
            }
            return total;
        }
    }
}
