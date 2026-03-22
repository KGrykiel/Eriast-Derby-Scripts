using Assets.Scripts.Characters;
using Assets.Scripts.Combat.Rolls;
using Assets.Scripts.Combat.Rolls.RollSpecs.SpecTypes;
using Assets.Scripts.Entities.Vehicle;

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
            public RollActor Actor;

            /// <summary>Whether the check can be attempted at all.</summary>
            public bool CanAttempt;

            /// <summary>Why the check can't be attempted (for UI/narrative). Null if CanAttempt is true.</summary>
            public string FailureReason;

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

        /// <param name="initiatingCharacter">Null for vehicle-wide checks (event cards, lane effects).</param>
        public static RoutingResult RouteSkillCheck(Vehicle vehicle, SkillCheckSpec spec, Character initiatingCharacter = null)
        {
            if (vehicle == null)
                return RoutingResult.Failure("No vehicle");

            if (spec is CharacterSkillCheckSpec charSpec)
                return RouteCharacterSkillCheck(vehicle, charSpec, initiatingCharacter);

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
        {
            Entity component = GetComponentForVehicleAttribute(vehicle, spec.vehicleAttribute);
            if (component == null)
                return RoutingResult.Failure($"No component available for {spec.DisplayName}");

            return RoutingResult.Success(new ComponentActor(component));
        }

        private static RoutingResult RouteVehicleSave(Vehicle vehicle, VehicleSaveSpec spec)
        {
            Entity component = GetComponentForVehicleAttribute(vehicle, spec.vehicleAttribute);
            if (component == null)
                return RoutingResult.Failure($"No component available for {spec.DisplayName} save");

            return RoutingResult.Success(new ComponentActor(component));
        }

        // ==================== CHARACTER CHECK ROUTING ====================

        private static RoutingResult RouteCharacterSkillCheck(Vehicle vehicle, CharacterSkillCheckSpec spec, Character initiatingCharacter)
        {
            // If check requires a specific component type, validate it and find operator
            if (spec.RequiresComponent)
            {
                return ValidateRequiredComponent(vehicle, spec.requiredComponentType);
            }

            // Priority 1: Specific character initiated this skill (character-initiated skills)
            if (initiatingCharacter != null)
            {
                return RoutingResult.Success(new CharacterActor(initiatingCharacter));
            }

            // Priority 2: No specific character - use character with best modifier (event cards, lane effects)
            Character bestCharacter = GetCharacterWithBestSkillModifier(vehicle, spec.characterSkill);
            if (bestCharacter != null)
                return RoutingResult.Success(new CharacterActor(bestCharacter));

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
                return ValidateRequiredComponent(vehicle, spec.requiredComponentType);
            }

            // No component required - route based on context

            // Priority 1: Target component specified (attacked location)
            if (targetComponent != null && targetComponent.IsOperational)
            {
                var seat = vehicle.GetSeatForComponent(targetComponent);
                Character character = seat != null ? seat.assignedCharacter : null;
                if (character != null)
                    return RoutingResult.Success(new CharacterWithToolActor(character, targetComponent));
            }

            // Priority 2: Best modifier for the save attribute
            Character bestCharacter = GetCharacterWithBestSaveModifier(vehicle, spec.characterAttribute);
            if (bestCharacter != null)
                return RoutingResult.Success(new CharacterActor(bestCharacter));

            // Priority 3: First assigned character (fallback)
            Character fallbackCharacter = GetFirstAssignedCharacter(vehicle);
            if (fallbackCharacter != null)
                return RoutingResult.Success(new CharacterActor(fallbackCharacter));

            return RoutingResult.Failure("No character available for save");
        }
        
        // ==================== COMPONENT ROUTING ====================
        
        private static Entity GetComponentForVehicleAttribute(Vehicle vehicle, VehicleCheckAttribute checkAttr)
        {
            Attribute attribute = checkAttr.ToAttribute();
            return attribute switch
            {
                Attribute.Mobility  => vehicle.chassis,
                Attribute.Integrity => vehicle.chassis,
                _ => null
            };
        }
        
        // ==================== CHARACTER HELPERS ====================
        
        private static Character GetCharacterWithBestSkillModifier(
            Vehicle vehicle,
            CharacterSkill skill)
        {
            Character best = null;
            int bestModifier = int.MinValue;

            foreach (var seat in vehicle.seats)
            {
                var character = seat?.assignedCharacter;
                if (character == null) continue;

                int modifier = CharacterFormulas.CalculateSkillCheckModifier(character, skill);
                if (modifier > bestModifier)
                {
                    best = character;
                    bestModifier = modifier;
                }
            }

            return best;
        }
        
        private static Character GetFirstAssignedCharacter(Vehicle vehicle)
        {
            foreach (var seat in vehicle.seats)
            {
                if (seat?.assignedCharacter != null)
                    return seat.assignedCharacter;
            }
            return null;
        }
        
        private static Character GetCharacterWithBestSaveModifier(
            Vehicle vehicle,
            CharacterAttribute attribute)
        {
            Character best = null;
            int bestModifier = int.MinValue;

            foreach (var seat in vehicle.seats)
            {
                var character = seat?.assignedCharacter;
                if (character == null) continue;

                int modifier = CharacterFormulas.CalculateSaveModifier(character, attribute);
                if (modifier > bestModifier)
                {
                    best = character;
                    bestModifier = modifier;
                }
            }

            return best;
        }
        
        private static VehicleComponent GetComponentByType(Vehicle vehicle, ComponentType type)
        {
            foreach (var component in vehicle.AllComponents)
            {
                if (component != null && component.componentType == type && component.IsOperational)
                    return component;
            }
            return null;
        }

        /// <summary>Validates component exists, is operational, has a seat, and has a character.</summary>
        private static RoutingResult ValidateRequiredComponent(Vehicle vehicle, ComponentType requiredType)
        {
            VehicleComponent component = GetComponentByType(vehicle, requiredType);
            if (component == null)
                return RoutingResult.Failure($"No {requiredType} component on vehicle");

            if (!component.IsOperational)
                return RoutingResult.Failure($"{component.name} is not operational");

            var seat = vehicle.GetSeatForComponent(component);
            if (seat == null)
                return RoutingResult.Failure($"No seat controls {component.name}");

            Character character = seat.assignedCharacter;
            if (character == null)
                return RoutingResult.Failure($"{seat.seatName} has no assigned character");

            return RoutingResult.Success(new CharacterWithToolActor(character, component));
        }
    }
}
