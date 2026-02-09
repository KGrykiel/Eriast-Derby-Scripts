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
            public PlayerCharacter Character;
            
            /// <summary>Whether the check can be attempted at all.</summary>
            public bool CanAttempt;
            
            /// <summary>Why the check can't be attempted (for UI/narrative). Null if CanAttempt is true.</summary>
            public string FailureReason;
            
            public static Resolution Success(Entity component, PlayerCharacter character = null)
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
            RoleType preferredRole = RoleType.None,
            VehicleComponent targetComponent = null)
        {
            if (vehicle == null)
                return Resolution.Failure("No vehicle");
            
            if (spec.IsCharacterSave)
                return ResolveCharacterSave(vehicle, spec, preferredRole, targetComponent);
            
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
            VehicleComponent component = GetComponentForCharacterSkill(vehicle, spec.characterSkill);
            if (component == null)
                return Resolution.Failure($"No component available for {spec.DisplayName}");
            
            if (!component.IsOperational)
                return Resolution.Failure($"{component.name} is not operational");
            
            var seat = vehicle.GetSeatForComponent(component);
            if (seat == null)
                return Resolution.Failure($"No seat controls {component.name}");
            
            PlayerCharacter character = seat.assignedCharacter;
            if (character == null)
                return Resolution.Failure($"{seat.seatName} has no assigned character");
            
            return Resolution.Success(component, character);
        }
        
        private static Resolution ResolveCharacterSave(
            Vehicle vehicle,
            SaveSpec spec,
            RoleType preferredRole,
            VehicleComponent targetComponent)
        {
            PlayerCharacter character = null;
            
            // Priority 1: Preferred role specified (threat-type routing)
            if (preferredRole != RoleType.None)
            {
                character = GetCharacterWithRole(vehicle, preferredRole);
                if (character != null)
                    return Resolution.Success(null, character);
            }
            
            // Priority 2: Target component specified (target-location routing)
            if (targetComponent != null && targetComponent.IsOperational)
            {
                var seat = vehicle.GetSeatForComponent(targetComponent);
                character = seat?.assignedCharacter;
                if (character != null)
                    return Resolution.Success(targetComponent, character);
            }
            
            // Priority 3: Best modifier for the save attribute
            character = GetCharacterWithBestSaveModifier(vehicle, spec.characterAttribute);
            if (character != null)
                return Resolution.Success(null, character);
            
            // Priority 4: First assigned character (fallback)
            character = GetFirstAssignedCharacter(vehicle);
            if (character == null)
                return Resolution.Failure("No character available for save");
            
            return Resolution.Success(null, character);
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
        
        /// <summary>
        /// Get the component a character skill requires.
        /// </summary>
        private static VehicleComponent GetComponentForCharacterSkill(Vehicle vehicle, CharacterSkill skill)
        {
            return skill switch
            {
                // Driver skills → chassis
                CharacterSkill.Piloting or
                CharacterSkill.DefensiveManeuvers or
                CharacterSkill.Stunts
                    => vehicle.chassis,
                
                // Future: Navigator skills → sensor component
                // Future: Engineer skills → drive/power core component
                // Future: Gunner skills → weapon component
                
                _ => null
            };
        }
        
        /// <summary>
        /// Get the first assigned character on the vehicle.
        /// Future: pick best character for the specific save type.
        /// </summary>
        private static PlayerCharacter GetFirstAssignedCharacter(Vehicle vehicle)
        {
            foreach (var seat in vehicle.seats)
            {
                if (seat?.assignedCharacter != null)
                    return seat.assignedCharacter;
            }
            return null;
        }
        
        /// <summary>
        /// Get a character with the specified role.
        /// Returns the first character found with that role.
        /// </summary>
        private static PlayerCharacter GetCharacterWithRole(Vehicle vehicle, RoleType role)
        {
            foreach (var seat in vehicle.seats)
            {
                if (seat?.assignedCharacter != null && seat.HasRole(role))
                    return seat.assignedCharacter;
            }
            return null;
        }
        
        /// <summary>
        /// Get the character with the best modifier for a given attribute.
        /// Useful for vehicle-wide saves where no specific role or target exists.
        /// </summary>
        private static PlayerCharacter GetCharacterWithBestSaveModifier(
            Vehicle vehicle,
            CharacterAttribute attribute)
        {
            PlayerCharacter best = null;
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
    }
}
