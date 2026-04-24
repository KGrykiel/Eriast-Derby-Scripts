using System;
using Assets.Scripts.Characters;
using Assets.Scripts.Combat.Rolls;
using Assets.Scripts.Combat.Rolls.RollSpecs.SpecTypes;
using Assets.Scripts.Core;
using Assets.Scripts.Entities;
using Assets.Scripts.Entities.Vehicles;
using Assets.Scripts.Entities.Vehicles.VehicleComponents;

namespace Assets.Scripts.Combat
{
    /// <summary>
    /// Vehicle-only routing: resolves which component and/or character makes a check.
    /// Exists because vehicles have internal complexity (components + crew).
    /// Standalone entities bypass this entirely — they call Calculator directly.
    /// Does NOT calculate bonuses, roll dice, or emit events.
    /// </summary>
    public static class CheckRouter
    {
        /// <summary>
        /// Result of routing a check. Contains everything the calculator needs.
        /// </summary>
        public class RoutingResult
        {
            /// <summary>Who or what makes the roll.</summary>
            public RollActor Actor { get; private set; }

            /// <summary>Whether the check can be attempted at all.</summary>
            public bool CanAttempt { get; private set; }

            /// <summary>Why the check can't be attempted (for UI/narrative). Null if CanAttempt is true.</summary>
            public string FailureReason { get; private set; }

            public static RoutingResult Success(RollActor actor)
            {
                return new RoutingResult
                {
                    Actor = actor,
                    CanAttempt = true
                };
            }

            public static RoutingResult Failure(string reason)
            {
                return new RoutingResult
                {
                    CanAttempt = false,
                    FailureReason = reason
                };
            }
        }
        
        // ==================== SKILL CHECK ROUTING ====================

        /// <param name="actorHint">Actor initiating the check (for character-initiated skills). Null for vehicle-wide checks.</param>
        public static RoutingResult RouteSkillCheck(Vehicle vehicle, SkillCheckSpec spec, RollActor actorHint = null)
        {
            if (vehicle == null)
                return RoutingResult.Failure("No vehicle");

            if (spec is CharacterSkillCheckSpec charSpec)
                return RouteCharacterSkillCheck(vehicle, charSpec, actorHint);

            if (spec is VehicleSkillCheckSpec vehicleSpec)
                return RouteVehicleCheck(vehicle, vehicleSpec);

            return RoutingResult.Failure("Unknown skill check spec type");
        }

        public static RoutingResult RouteSave(
            Vehicle vehicle, 
            SaveSpec spec,
            VehicleComponent targetComponent = null)
        {
            if (vehicle == null)
                return RoutingResult.Failure("No vehicle");

            if (spec is CharacterSaveSpec charSpec)
                return RouteCharacterSave(vehicle, charSpec, targetComponent);

            if (spec is VehicleSaveSpec vehicleSpec)
                return RouteVehicleSave(vehicle, vehicleSpec);

            return RoutingResult.Failure("Unknown save spec type");
        }

        // ==================== VEHICLE CHECK ROUTING ====================

        private static RoutingResult RouteVehicleCheck(Vehicle vehicle, VehicleSkillCheckSpec spec)
            => RouteVehicleRoll(vehicle, spec.vehicleAttribute, spec.DisplayName);

        private static RoutingResult RouteVehicleSave(Vehicle vehicle, VehicleSaveSpec spec)
            => RouteVehicleRoll(vehicle, spec.vehicleAttribute, spec.DisplayName);

        private static RoutingResult RouteVehicleRoll(Vehicle vehicle, VehicleCheckAttribute attr, string displayName)
        {
            Entity component = VehicleComponentResolver.ResolveForAttribute(vehicle, attr.ToAttribute());
            if (component == null)
                return RoutingResult.Failure($"No component available for {displayName}");

            return RoutingResult.Success(new ComponentActor(component));
        }

        // ==================== CHARACTER CHECK ROUTING ====================

        private static RoutingResult RouteCharacterSkillCheck(Vehicle vehicle, CharacterSkillCheckSpec spec, RollActor actorHint)
        {
            // If check requires a specific component type, validate it and find operator
            if (spec.RequiresComponent)
            {
                return ValidateRequiredRole(vehicle, spec.requiredRole);
            }

            // Priority 1: Actor hint provided (character-initiated skills)
            if (actorHint != null)
            {
                return RoutingResult.Success(actorHint);
            }

            // Priority 2: No specific character - use character with best modifier (event cards, lane effects)
            VehicleSeat bestSeat = GetSeatWithBestSkillModifier(vehicle, spec.characterSkill);
            if (bestSeat != null)
                return RoutingResult.Success(new CharacterActor(bestSeat));

            return RoutingResult.Failure($"No character available for {spec.DisplayName}");
        }

        private static RoutingResult RouteCharacterSave(
            Vehicle vehicle,
            CharacterSaveSpec spec,
            VehicleComponent targetComponent)
        {
            // If save requires a specific component type, validate it and find operator
            if (spec.RequiresComponent)
            {
                return ValidateRequiredRole(vehicle, spec.requiredRole);
            }

            // No component required - route based on context

            // Priority 1: Target component specified (attacked location)
            if (targetComponent != null && targetComponent.IsOperational)
            {
                var seat = vehicle.GetSeatForComponent(targetComponent);
                if (seat != null && seat.IsAssigned)
                    return RoutingResult.Success(new CharacterWithToolActor(seat, targetComponent));
            }

            // Priority 2: Best modifier for the save attribute
            VehicleSeat bestSeat = GetSeatWithBestSaveModifier(vehicle, spec.characterAttribute);
            if (bestSeat != null)
                return RoutingResult.Success(new CharacterActor(bestSeat));

            return RoutingResult.Failure("No character available for save");
        }
        
        // ==================== CHARACTER HELPERS ====================

        private static VehicleSeat GetSeatWithBestSkillModifier(Vehicle vehicle, CharacterSkill skill)
            => GetSeatWithBestModifier(vehicle, seat => CharacterStatCalculator.GatherSkillValue(seat, skill));

        private static VehicleSeat GetSeatWithBestSaveModifier(Vehicle vehicle, CharacterAttribute attribute)
            => GetSeatWithBestModifier(vehicle, seat => CharacterStatCalculator.GatherSaveValue(seat, attribute));

        private static VehicleSeat GetSeatWithBestModifier(Vehicle vehicle, Func<VehicleSeat, int> getModifier)
        {
            VehicleSeat best = null;
            int bestModifier = int.MinValue;

            foreach (var seat in vehicle.seats)
            {
                if (seat == null || !seat.IsAssigned) continue;

                int modifier = getModifier(seat);
                if (modifier > bestModifier)
                {
                    best = seat;
                    bestModifier = modifier;
                }
            }

            return best;
        }
        
        private static VehicleComponent GetComponentByRole(Vehicle vehicle, RoleType role)
        {
            foreach (var component in vehicle.AllComponents)
            {
                if (component != null && (component.roleType & role) != 0)
                    return component;
            }
            return null;
        }

        /// <summary>Validates component exists, is operational, has a seat, and has a character.</summary>
        private static RoutingResult ValidateRequiredRole(Vehicle vehicle, RoleType requiredRole)
        {
            VehicleComponent component = GetComponentByRole(vehicle, requiredRole);
            if (component == null)
                return RoutingResult.Failure($"No {requiredRole} component on vehicle");

            if (!component.IsOperational)
                return RoutingResult.Failure($"{component.name} is not operational");

            var seat = vehicle.GetSeatForComponent(component);
            if (seat == null)
                return RoutingResult.Failure($"No seat controls {component.name}");

            if (!seat.IsAssigned)
                return RoutingResult.Failure($"{seat.seatName} has no assigned character");

            return RoutingResult.Success(new CharacterWithToolActor(seat, component));
        }
    }
}
