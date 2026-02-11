using Assets.Scripts.Characters;
using Assets.Scripts.Combat.Saves;
using Assets.Scripts.Combat.SkillChecks;
using Assets.Scripts.Entities.Vehicle;

namespace Assets.Scripts.Combat
{
    /// <summary>
    /// VEHICLE-ONLY routing for checks and saves.
    /// Resolves which component and/or character should make a check when the target is a Vehicle.
    /// 
    /// SCOPE: This class exists ONLY because vehicles have internal complexity (components + crew).
    /// - Standalone entities (NPCs, props) bypass this entirely - they call Calculator directly.
    /// - Performers handle the decision: Vehicle → CheckRouter → Calculator, Entity → Calculator.
    /// 
    /// RESPONSIBILITIES:
    /// - Find the correct component for vehicle attribute checks/saves
    /// - Find the best character for character checks/saves (considering proficiency/attributes)
    /// - Validate component existence and operational state
    /// - Validate character availability (seat assignments)
    /// 
    /// DOES NOT: Calculate bonuses, roll dice, emit events (that's Calculator/Performer territory).
    /// </summary>
    public static class CheckRouter
    {
        /// <summary>
        /// Result of routing a check. Contains everything the calculator needs.
        /// </summary>
        public class RoutingResult
        {
            /// <summary>The component involved (for base value + applied modifiers). Null if none.</summary>
            public Entity Component;
            
            /// <summary>The character making the check (for attr mod + proficiency). Null for vehicle-only checks.</summary>
            public Character Character;
            
            /// <summary>Whether the check can be attempted at all.</summary>
            public bool CanAttempt;
            
            /// <summary>Why the check can't be attempted (for UI/narrative). Null if CanAttempt is true.</summary>
            public string FailureReason;
            
            public static RoutingResult Success(Entity component, Character character = null)
            {
                return new RoutingResult
                {
                    Component = component,
                    Character = character,
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

        /// <summary>
        /// Route a skill check: which component and character are involved?
        /// </summary>
        /// <param name="vehicle">Vehicle attempting the check</param>
        /// <param name="spec">What type of check is being made</param>
        /// <param name="initiatingCharacter">Character who initiated this check (for character-initiated skills). Null for vehicle-wide checks like event cards.</param>
        public static RoutingResult RouteSkillCheck(Vehicle vehicle, SkillCheckSpec spec, Character initiatingCharacter = null)
        {
            if (vehicle == null)
                return RoutingResult.Failure("No vehicle");

            if (spec.IsCharacterCheck)
                return RouteCharacterSkillCheck(vehicle, spec, initiatingCharacter);

            return RouteVehicleCheck(vehicle, spec);
        }

        /// <summary>
        /// Route a saving throw: which component and character are involved?
        /// </summary>
        public static RoutingResult RouteSave(
            Vehicle vehicle, 
            SaveSpec spec,
            VehicleComponent targetComponent = null)
        {
            if (vehicle == null)
                return RoutingResult.Failure("No vehicle");

            if (spec.IsCharacterSave)
                return RouteCharacterSave(vehicle, spec, targetComponent);

            return RouteVehicleSave(vehicle, spec);
        }

        // ==================== VEHICLE CHECK ROUTING ====================

        private static RoutingResult RouteVehicleCheck(Vehicle vehicle, SkillCheckSpec spec)
        {
            Entity component = GetComponentForVehicleAttribute(vehicle, spec.vehicleAttribute);
            if (component == null)
                return RoutingResult.Failure($"No component available for {spec.DisplayName}");

            return RoutingResult.Success(component);
        }

        private static RoutingResult RouteVehicleSave(Vehicle vehicle, SaveSpec spec)
        {
            Entity component = GetComponentForVehicleAttribute(vehicle, spec.vehicleAttribute);
            if (component == null)
                return RoutingResult.Failure($"No component available for {spec.DisplayName} save");

            return RoutingResult.Success(component);
        }

        // ==================== CHARACTER CHECK ROUTING ====================

        private static RoutingResult RouteCharacterSkillCheck(Vehicle vehicle, SkillCheckSpec spec, Character initiatingCharacter)
        {
            // If check requires a specific component type, validate it and find operator
            if (spec.RequiresComponent)
            {
                return ValidateRequiredComponent(vehicle, spec.requiredComponentType);
            }

            // Priority 1: Specific character initiated this skill (character-initiated skills)
            if (initiatingCharacter != null)
            {
                return RoutingResult.Success(null, initiatingCharacter);
            }

            // Priority 2: No specific character - use character with best modifier (event cards, lane effects)
            Character bestCharacter = GetCharacterWithBestSkillModifier(vehicle, spec.characterSkill);
            if (bestCharacter != null)
                return RoutingResult.Success(null, bestCharacter);

            return RoutingResult.Failure($"No character available for {spec.DisplayName}");
        }

        private static RoutingResult RouteCharacterSave(
            Vehicle vehicle,
            SaveSpec spec,
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
                Character character = seat?.assignedCharacter;
                if (character != null)
                    return RoutingResult.Success(targetComponent, character);
            }

            // Priority 2: Best modifier for the save attribute
            Character bestCharacter = GetCharacterWithBestSaveModifier(vehicle, spec.characterAttribute);
            if (bestCharacter != null)
                return RoutingResult.Success(null, bestCharacter);

            // Priority 3: First assigned character (fallback)
            Character fallbackCharacter = GetFirstAssignedCharacter(vehicle);
            if (fallbackCharacter != null)
                return RoutingResult.Success(null, fallbackCharacter);

            return RoutingResult.Failure("No character available for save");
        }
        
        // ==================== COMPONENT ROUTING ====================
        
        /// <summary>
        /// Get the component responsible for a vehicle attribute (shared by checks and saves).
        /// </summary>
        private static Entity GetComponentForVehicleAttribute(Vehicle vehicle, VehicleCheckAttribute checkAttr)
        {
            Attribute attribute = checkAttr.ToAttribute();
            return attribute switch
            {
                Attribute.Mobility => vehicle.chassis,
                _ => null
            };
        }
        
        // ==================== CHARACTER HELPERS ====================
        
        /// <summary>
        /// Get the character with the best modifier for a given skill.
        /// Used for component-optional checks where any character can attempt.
        /// </summary>
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
        
        /// <summary>
        /// Get the character with the best modifier for a given attribute.
        /// Useful for vehicle-wide saves where no specific role or target exists.
        /// </summary>
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
        
        /// <summary>
        /// Get a component of a specific type from the vehicle.
        /// Returns the first operational component of that type found.
        /// </summary>
        private static VehicleComponent GetComponentByType(Vehicle vehicle, ComponentType type)
        {
            foreach (var component in vehicle.AllComponents)
            {
                if (component != null && component.componentType == type && component.IsOperational)
                    return component;
            }
            return null;
        }

        /// <summary>
        /// Validate that a required component exists, is operational, has a controlling seat, and has an assigned character.
        /// Extracted common logic used by both character skill checks and character saves.
        /// Returns Success with component+character if valid, otherwise returns Failure with reason.
        /// </summary>
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

            return RoutingResult.Success(component, character);
        }
    }
}
