using Assets.Scripts.Characters;
using Assets.Scripts.Combat.Saves;
using Assets.Scripts.Combat.SkillChecks;
using Assets.Scripts.Entities.Vehicle;

namespace Assets.Scripts.Combat
{
    /// <summary>
    /// Resolves who and what is involved in a check or save.
    /// 
    /// Given a vehicle and a spec, determines:
    /// - Which component is needed (and whether it's functional)
    /// - Which character operates it (from seat assignment)
    /// - Whether the attempt is even possible
    /// 
    /// The calculator receives the resolved data — it never routes.
    /// </summary>
    public static class CheckResolver
    {
        /// <summary>
        /// Result of resolving a check. Contains everything the calculator needs.
        /// </summary>
        public class Resolution
        {
            /// <summary>The component involved (for base value + applied modifiers). Null if none.</summary>
            public Entity Component;
            
            /// <summary>The character making the check (for attr mod + proficiency). Null for vehicle-only checks.</summary>
            public Character Character;
            
            /// <summary>Whether the check can be attempted at all.</summary>
            public bool CanAttempt;
            
            /// <summary>Why the check can't be attempted (for UI/narrative). Null if CanAttempt is true.</summary>
            public string FailureReason;
            
            public static Resolution Success(Entity component, Character character = null)
            {
                return new Resolution
                {
                    Component = component,
                    Character = character,
                    CanAttempt = true
                };
            }
            
            public static Resolution Failure(string reason)
            {
                return new Resolution
                {
                    CanAttempt = false,
                    FailureReason = reason
                };
            }
        }
        
        // ==================== SKILL CHECK RESOLUTION ====================
        
        /// <summary>
        /// Resolve a skill check: which component and character are involved?
        /// </summary>
        public static Resolution ResolveSkillCheck(Vehicle vehicle, CheckSpec spec)
        {
            if (vehicle == null)
                return Resolution.Failure("No vehicle");
            
            if (spec.IsCharacterCheck)
                return ResolveCharacterSkillCheck(vehicle, spec);
            
            return ResolveVehicleCheck(vehicle, spec);
        }
        
        /// <summary>
        /// Resolve a saving throw: which component and character are involved?
        /// </summary>
        public static Resolution ResolveSave(
            Vehicle vehicle, 
            SaveSpec spec,
            VehicleComponent targetComponent = null)
        {
            if (vehicle == null)
                return Resolution.Failure("No vehicle");
            
            if (spec.IsCharacterSave)
                return ResolveCharacterSave(vehicle, spec, targetComponent);
            
            return ResolveVehicleSave(vehicle, spec);
        }
        
        // ==================== VEHICLE CHECKS ====================
        
        private static Resolution ResolveVehicleCheck(Vehicle vehicle, CheckSpec spec)
        {
            Entity component = GetComponentForVehicleAttribute(vehicle, spec.vehicleAttribute);
            if (component == null)
                return Resolution.Failure($"No component available for {spec.DisplayName}");
            
            return Resolution.Success(component);
        }
        
        private static Resolution ResolveVehicleSave(Vehicle vehicle, SaveSpec spec)
        {
            Entity component = GetComponentForVehicleAttribute(vehicle, spec.vehicleAttribute);
            if (component == null)
                return Resolution.Failure($"No component available for {spec.DisplayName} save");
            
            return Resolution.Success(component);
        }
        
        // ==================== CHARACTER CHECKS ====================
        
        private static Resolution ResolveCharacterSkillCheck(Vehicle vehicle, CheckSpec spec)
        {
            // If check requires a specific component type, find it and its operator
            if (spec.RequiresComponent)
            {
                VehicleComponent component = GetComponentByType(vehicle, spec.requiredComponentType.Value);
                if (component == null)
                    return Resolution.Failure($"No {spec.requiredComponentType.Value} component on vehicle");
                
                if (!component.IsOperational)
                    return Resolution.Failure($"{component.name} is not operational");
                
                var seat = vehicle.GetSeatForComponent(component);
                if (seat == null)
                    return Resolution.Failure($"No seat controls {component.name}");
                
                Character character = seat.assignedCharacter;
                if (character == null)
                    return Resolution.Failure($"{seat.seatName} has no assigned character");
                
                return Resolution.Success(component, character);
            }
            
            // No component required - use character with best modifier for this skill
            Character bestCharacter = GetCharacterWithBestSkillModifier(vehicle, spec.characterSkill);
            if (bestCharacter == null)
                return Resolution.Failure($"No character available for {spec.DisplayName}");
            
            return Resolution.Success(null, bestCharacter);
        }
        
        private static Resolution ResolveCharacterSave(
            Vehicle vehicle,
            SaveSpec spec,
            VehicleComponent targetComponent)
        {
            // If save requires a specific component type, find it and its operator
            if (spec.RequiresComponent)
            {
                VehicleComponent component = GetComponentByType(vehicle, spec.requiredComponentType.Value);
                if (component == null)
                    return Resolution.Failure($"No {spec.requiredComponentType.Value} component on vehicle");
                
                if (!component.IsOperational)
                    return Resolution.Failure($"{component.name} is not operational");
                
                var seat = vehicle.GetSeatForComponent(component);
                if (seat == null)
                    return Resolution.Failure($"No seat controls {component.name}");
                
                Character character = seat.assignedCharacter;
                if (character == null)
                    return Resolution.Failure($"{seat.seatName} has no assigned character");
                
                return Resolution.Success(component, character);
            }
            
            // No component required - route based on context
            Character resolvedCharacter = null;
            
            // Priority 1: Target component specified (attacked location)
            if (targetComponent != null && targetComponent.IsOperational)
            {
                var seat = vehicle.GetSeatForComponent(targetComponent);
                resolvedCharacter = seat?.assignedCharacter;
                if (resolvedCharacter != null)
                    return Resolution.Success(targetComponent, resolvedCharacter);
            }
            
            // Priority 2: Best modifier for the save attribute
            resolvedCharacter = GetCharacterWithBestSaveModifier(vehicle, spec.characterAttribute);
            if (resolvedCharacter != null)
                return Resolution.Success(null, resolvedCharacter);
            
            // Priority 3: First assigned character (fallback)
            resolvedCharacter = GetFirstAssignedCharacter(vehicle);
            if (resolvedCharacter == null)
                return Resolution.Failure("No character available for save");
            
            return Resolution.Success(null, resolvedCharacter);
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
                
                int modifier = character.GetAttributeModifier(attribute) + character.GetProficiencyBonus(skill);
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
                
                int modifier = character.GetAttributeModifier(attribute) + (character.level / 2);
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
                if (component != null && component.componentType == type)
                    return component;
            }
            return null;
        }
    }
}
