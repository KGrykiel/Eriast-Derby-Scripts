using System.Linq;
using Assets.Scripts.StatusEffects;

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
        public static string FormatSource(Character character, Entity entity, Vehicle vehicle)
        {
            if (vehicle == null)
                vehicle = EntityHelpers.GetParentVehicle(entity);

            string componentName = (entity is VehicleComponent comp) ? comp.name : null;
            string vehicleName = vehicle != null ? vehicle.vehicleName : null;

            if (character != null)
            {
                string suffix = BuildSuffix(componentName, vehicleName, "via");
                return $"{character.characterName}{suffix}";
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
        public static string FormatDefensiveSource(Character character, Entity entity, Vehicle vehicle)
        {
            if (vehicle == null)
                vehicle = EntityHelpers.GetParentVehicle(entity);

            string componentName = (entity is VehicleComponent comp) ? comp.name : null;
            string vehicleName = vehicle != null ? vehicle.vehicleName : null;

            if (character != null)
            {
                string suffix = BuildSuffix(componentName, vehicleName, "at");
                return $"{character.characterName}{suffix}";
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
            return FormatSource(action.SourceCharacter, action.Actor, action.ActorVehicle);
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

        public static bool DetermineIfBuff(StatusEffect statusEffect)
        {
            float totalModifierValue = statusEffect.modifiers.Sum(m => m.value);

            bool hasPeriodicDamage = statusEffect.periodicEffects.Any(p => p.type == PeriodicEffectType.Damage);
            bool hasPeriodicHealing = statusEffect.periodicEffects.Any(p => p.type == PeriodicEffectType.Healing);
            bool hasEnergyDrain = statusEffect.periodicEffects.Any(p => p.type == PeriodicEffectType.EnergyDrain);
            bool hasEnergyRestore = statusEffect.periodicEffects.Any(p => p.type == PeriodicEffectType.EnergyRestore);

            bool hasBehavioralRestrictions = statusEffect.behavioralEffects != null &&
                (statusEffect.behavioralEffects.preventsActions ||
                 statusEffect.behavioralEffects.preventsMovement ||
                 statusEffect.behavioralEffects.damageAmplification > 1f);

            if (hasPeriodicDamage || hasEnergyDrain || hasBehavioralRestrictions)
                return false;
            if (hasPeriodicHealing || hasEnergyRestore || totalModifierValue > 0)
                return true;
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
