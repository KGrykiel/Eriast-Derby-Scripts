using Assets.Scripts.Characters;
using Assets.Scripts.Combat.Saves;
using Assets.Scripts.Combat.SkillChecks;
using Assets.Scripts.Entities.Vehicle;

namespace Assets.Scripts.Combat
{
    /// <summary>
    /// Routes who and what is involved in a check or save.
    /// 
    /// Given a vehicle and a spec, determines:
    /// - Which component is needed (and whether it's functional)
    /// - Which character operates it (from seat assignment)
    /// - Whether the attempt is even possible
    /// 
    /// The calculator receives the routed data — it never routes.
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
            // If check requires a specific component type, find it and its operator
            if (spec.RequiresComponent)
            {
                VehicleComponent component = GetComponentByType(vehicle, spec.requiredComponentType);
                if (component == null)
                    return RoutingResult.Failure($"No {spec.requiredComponentType} component on vehicle");

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

            // Priority 1: Specific character initiated this skill (character-initiated skills)
            if (initiatingCharacter != null)
            {
                return RoutingResult.Success(null, initiatingCharacter);
            }

            // Priority 2: No specific character - use character with best modifier (event cards, lane effects)
            Character bestCharacter = GetCharacterWithBestSkillModifier(vehicle, spec.characterSkill);
            if (bestCharacter == null)
                return RoutingResult.Failure($"No character available for {spec.DisplayName}");

            return RoutingResult.Success(null, bestCharacter);
        }

        private static RoutingResult RouteCharacterSave(
            Vehicle vehicle,
            SaveSpec spec,
            VehicleComponent targetComponent)
        {
            // If save requires a specific component type, find it and its operator
            if (spec.RequiresComponent)
            {
                VehicleComponent component = GetComponentByType(vehicle, spec.requiredComponentType);
                if (component == null)
                    return RoutingResult.Failure($"No {spec.requiredComponentType} component on vehicle");

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

            // No component required - route based on context
            Character resolvedCharacter = null;

            // Priority 1: Target component specified (attacked location)
            if (targetComponent != null && targetComponent.IsOperational)
            {
                var seat = vehicle.GetSeatForComponent(targetComponent);
                resolvedCharacter = seat?.assignedCharacter;
                if (resolvedCharacter != null)
                    return RoutingResult.Success(targetComponent, resolvedCharacter);
            }

            // Priority 2: Best modifier for the save attribute
            resolvedCharacter = GetCharacterWithBestSaveModifier(vehicle, spec.characterAttribute);
            if (resolvedCharacter != null)
                return RoutingResult.Success(null, resolvedCharacter);

            // Priority 3: First assigned character (fallback)
            resolvedCharacter = GetFirstAssignedCharacter(vehicle);
            if (resolvedCharacter == null)
                return RoutingResult.Failure("No character available for save");

            return RoutingResult.Success(null, resolvedCharacter);
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
            
            CharacterAttribute attribute = CharacterSkillHelper.GetPrimaryAttribute(skill);
            
            foreach (var seat in vehicle.seats)
            {
                var character = seat?.assignedCharacter;
                if (character == null) continue;

                int attributeScore = character.GetAttributeScore(attribute);
                int attrMod = CharacterFormulas.CalculateAttributeModifier(attributeScore);
                int proficiency = character.IsProficient(skill) 
                    ? CharacterFormulas.CalculateProficiencyBonus(character.level) 
                    : 0;
                int modifier = attrMod + proficiency;

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

                int attributeScore = character.GetAttributeScore(attribute);
                int attrMod = CharacterFormulas.CalculateAttributeModifier(attributeScore);
                int halfLevel = CharacterFormulas.CalculateHalfLevelBonus(character.level);
                int modifier = attrMod + halfLevel;

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
    }
}
