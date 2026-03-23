using System.Linq;
using Assets.Scripts.Combat.Rolls;
using Assets.Scripts.Conditions;
using Assets.Scripts.Conditions.EntityConditions;
using Assets.Scripts.Conditions.CharacterConditions;
using Assets.Scripts.Entities.Vehicle;

namespace Assets.Scripts.Combat.Logging
{
    /// <summary>
    /// Entity/vehicle name resolution for combat display.
    /// Determines how sources, targets, and actions are named in logs and UI.
    /// </summary>
    public static class CombatDisplayHelpers
    {
        // ==================== ENTITY DISPLAY NAMES ====================

        /// <summary>
        /// Format an entity with its parent vehicle in brackets.
        /// "Chassis [D1S Speedster]", "Stone Golem", "Ironclad", "Unknown"
        /// </summary>
        public static string FormatEntityWithVehicle(Entity entity, Vehicle parentVehicle = null)
        {
            if (entity == null)
                return parentVehicle != null ? parentVehicle.vehicleName : "Unknown";

            parentVehicle ??= EntityHelpers.GetParentVehicle(entity);

            if (entity is VehicleComponent component && parentVehicle != null)
                return $"{component.name} [{parentVehicle.vehicleName}]";

            return entity.GetDisplayName();
        }

        /// <summary>
        /// Format an active combat source (attacker, skill user).
        /// "Ada via Laser Cannon [Ironclad]", "Ada [Ironclad]", "Laser Cannon [Ironclad]", "Stone Golem"
        /// </summary>
        public static string FormatSource(VehicleSeat seat, Entity entity, Vehicle vehicle)
        {
            if (vehicle == null)
                vehicle = EntityHelpers.GetParentVehicle(entity);

            string componentName = (entity is VehicleComponent comp) ? comp.name : null;
            string vehicleName = vehicle != null ? vehicle.vehicleName : null;

            if (seat != null && seat.IsAssigned)
            {
                string suffix = BuildSuffix(componentName, vehicleName, "via");
                return $"{seat.GetDisplayName()}{suffix}";
            }

            if (entity != null)
            {
                if (componentName != null && vehicleName != null)
                    return $"{componentName} [{vehicleName}]";
                return entity.GetDisplayName();
            }

            return vehicleName ?? "Unknown";
        }

        /// <summary>
        /// Format a defensive source (save target).
        /// Uses "at" instead of "via" — the component is being defended, not used as a tool.
        /// "Ada at Chassis [Ironclad]", "Ada [Ironclad]", "Chassis [Ironclad]"
        /// </summary>
        public static string FormatDefensiveSource(VehicleSeat seat, Entity entity, Vehicle vehicle)
        {
            if (vehicle == null)
                vehicle = EntityHelpers.GetParentVehicle(entity);

            string componentName = (entity is VehicleComponent comp) ? comp.name : null;
            string vehicleName = vehicle != null ? vehicle.vehicleName : null;

            if (seat != null && seat.IsAssigned)
            {
                string suffix = BuildSuffix(componentName, vehicleName, "at");
                return $"{seat.GetDisplayName()}{suffix}";
            }

            if (entity != null)
            {
                if (componentName != null && vehicleName != null)
                    return $"{componentName} [{vehicleName}]";
                return entity.GetDisplayName();
            }

            return vehicleName ?? "Unknown";
        }

        public static string FormatActionSource(CombatAction action)
        {
            return FormatRollActor(action.SourceActor, action.ActorVehicle);
        }

        // ==================== ROLL ACTOR FORMATTERS ====================

        /// <summary>
        /// Universal formatter for an active RollActor (attacker, skill user).
        /// Resolves Character + Entity from the actor and delegates to FormatSource.
        /// </summary>
        public static string FormatRollActor(RollActor actor, Vehicle vehicle = null)
        {
            if (actor == null) return vehicle?.vehicleName ?? "Unknown";
            return FormatSource(actor.GetSeat(), actor.GetEntity(), vehicle);
        }

        /// <summary>
        /// Universal formatter for a defensive RollActor (save target).
        /// Resolves Character + Entity from the actor and delegates to FormatDefensiveSource.
        /// </summary>
        public static string FormatRollActorDefensive(RollActor actor, Vehicle vehicle = null)
        {
            if (actor == null) return vehicle?.vehicleName ?? "Unknown";
            return FormatDefensiveSource(actor.GetSeat(), actor.GetEntity(), vehicle);
        }

        // ==================== COMBAT LOGIC HELPERS ====================

        public static bool IsSelfTarget(Entity source, Entity target, Vehicle sourceVehicle, Vehicle targetVehicle)
        {
            if (source != null && source == target)
                return true;
            if (sourceVehicle != null && sourceVehicle == targetVehicle)
                return true;
            return false;
        }

        public static bool DetermineIfBuff(EntityCondition entityCondition)
        {
            float totalModifierValue = entityCondition.modifiers.Sum(m => m.value);

            bool hasPeriodicDamage = entityCondition.periodicEffects.Any(p => p is PeriodicDamageEffect);
            bool hasPeriodicRestoration = entityCondition.periodicEffects.Any(p =>
                p is PeriodicRestorationEffect pr && (pr.formula.baseDice > 0 || pr.formula.bonus > 0));
            bool hasPeriodicDrain = entityCondition.periodicEffects.Any(p =>
                p is PeriodicRestorationEffect pr && pr.formula.baseDice == 0 && pr.formula.bonus < 0);

            bool hasBehavioralRestrictions = entityCondition.behavioralEffects != null &&
                (entityCondition.behavioralEffects.preventsActions ||
                 entityCondition.behavioralEffects.preventsMovement);

            if (hasPeriodicDamage || hasPeriodicDrain || hasBehavioralRestrictions)
                return false;
            if (hasPeriodicRestoration || totalModifierValue > 0)
                return true;
            return totalModifierValue >= 0;
        }

        public static bool DetermineIfBuff(CharacterCondition condition)
        {
            float totalModifierValue = condition.modifiers.Sum(m => m.value);

            bool hasBehavioralRestrictions = condition.behavioralEffects != null &&
                (condition.behavioralEffects.preventsActions ||
                 condition.behavioralEffects.preventsMovement);

            if (hasBehavioralRestrictions)
                return false;
            return totalModifierValue >= 0;
        }

        // ==================== PRIVATE HELPERS ====================

        /// <summary>
        /// Build the bracketed suffix for a character-led action.
        /// " via Laser Cannon [Ironclad]", " at Chassis [Ironclad]", " [Ironclad]", ""
        /// </summary>
        private static string BuildSuffix(string componentName, string vehicleName, string preposition)
        {
            if (componentName != null && vehicleName != null)
                return $" {preposition} {componentName} [{vehicleName}]";
            if (vehicleName != null)
                return $" [{vehicleName}]";
            return "";
        }
    }
}
