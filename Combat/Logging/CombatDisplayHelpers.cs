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
        /// "Chassis [D1S Speedster]", "Stone Golem", "Unknown"
        /// </summary>
        public static string FormatEntityWithVehicle(Entity entity)
        {
            if (entity == null)
                return "Unknown";

            Vehicle parentVehicle = EntityHelpers.GetParentVehicle(entity);

            if (entity is VehicleComponent component && parentVehicle != null)
                return $"{component.name} [{parentVehicle.vehicleName}]";

            return entity.GetDisplayName();
        }

        /// <summary>
        /// Format a vehicle seat display name.
        /// Returns the character name if assigned, otherwise the seat name.
        /// </summary>
        public static string FormatSeatName(VehicleSeat seat)
        {
            if (seat == null) return "Unknown";
            return seat.GetDisplayName() ?? seat.seatName ?? "Unknown";
        }

        // ==================== ROLL ACTOR FORMATTERS ====================

        /// <summary>
        /// Format a RollActor display name.
        /// "Ada via Laser Cannon [Ironclad]", "Stone Golem", "Unknown"
        /// </summary>
        public static string FormatRollActor(RollActor actor)
        {
            if (actor == null) return "Unknown";

            Vehicle vehicle = actor.GetVehicle();
            return FormatActorDisplay(actor.GetSeat(), actor.GetEntity(), vehicle);
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

        private static string FormatActorDisplay(VehicleSeat seat, Entity entity, Vehicle vehicle)
        {
            string componentName = (entity is VehicleComponent comp) ? comp.name : null;
            string vehicleName = vehicle?.vehicleName;

            if (seat != null && seat.IsAssigned)
            {
                string suffix = BuildSuffix(componentName, vehicleName);
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
        /// Build the bracketed suffix for a character-led action.
        /// " via Laser Cannon [Ironclad]", " [Ironclad]", ""
        /// </summary>
        private static string BuildSuffix(string componentName, string vehicleName)
        {
            if (componentName != null && vehicleName != null)
                return $" via {componentName} [{vehicleName}]";
            if (vehicleName != null)
                return $" [{vehicleName}]";
            return "";
        }
    }
}
