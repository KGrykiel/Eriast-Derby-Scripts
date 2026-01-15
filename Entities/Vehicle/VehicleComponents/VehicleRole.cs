using System.Collections.Generic;
using System;
using System.Text;

namespace Entities.Vehicle.VehicleComponents
{
    /// <summary>
    /// Represents an emergent role discovered from vehicle components at runtime.
    /// Roles are NOT hardcoded - they emerge from components that enable them.
    /// This is a lightweight data structure, not a MonoBehaviour.
    /// </summary>
    [System.Serializable]
    public struct VehicleRole
    {
        /// <summary>
        /// Name of the role (e.g., "Driver", "Gunner", "Navigator", "Engineer", or custom).
        /// </summary>
        public string roleName;
        
        /// <summary>
        /// The component that enables this role.
        /// Example: A WeaponComponent enables a "Gunner" role.
        /// </summary>
        public VehicleComponent sourceComponent;
        
        /// <summary>
        /// The character assigned to operate this role (null if unassigned or AI).
        /// </summary>
        public PlayerCharacter assignedCharacter;
        
        /// <summary>
        /// All skills available to this role (component skills + character personal skills).
        /// </summary>
        public List<Skill> availableSkills;
        
        /// <summary>
        /// Has this role acted this turn?
        /// Convenience accessor to sourceComponent.hasActedThisTurn.
        /// </summary>
        public bool HasActed => sourceComponent != null && sourceComponent.hasActedThisTurn;
        
        /// <summary>
        /// Can this role currently act?
        /// Checks all conditions that would prevent action:
        /// - Component exists and is operational (not destroyed/disabled)
        /// - Character is assigned and available
        /// - Role hasn't acted this turn
        /// </summary>
        public bool CanAct
        {
            get
            {
                // Component must exist
                if (sourceComponent == null)
                    return false;
                
                // Component must not be destroyed
                if (sourceComponent.isDestroyed)
                    return false;
                
                // Component must not be disabled (by Engineer)
                if (sourceComponent.isDisabled)
                    return false;
                
                // Character must be assigned (for player vehicles)
                // Note: AI vehicles might not have characters assigned
                if (assignedCharacter == null)
                    return false;
                
                // TODO: Check if character is knocked out/incapacitated
                // This will be added when character status system is implemented
                // if (assignedCharacter.isKnockedOut) return false;
                
                // Role must not have already acted this turn
                if (sourceComponent.hasActedThisTurn)
                    return false;
                
                // All checks passed
                return true;
            }
        }
        
        /// <summary>
        /// Get a display name for this role (e.g., "Alice (Driver)").
        /// </summary>
        public string GetDisplayName()
        {
            if (assignedCharacter != null)
            {
                return $"{assignedCharacter.characterName} ({roleName})";
            }
            return $"[Unassigned] ({roleName})";
        }
        
        /// <summary>
        /// Get the reason why this role cannot act (for debugging/UI).
        /// Returns null if role can act.
        /// </summary>
        public string GetCannotActReason()
        {
            if (sourceComponent == null)
                return "Component missing";
            
            if (sourceComponent.isDestroyed)
                return $"{sourceComponent.name} is destroyed";
            
            if (sourceComponent.isDisabled)
                return $"{sourceComponent.name} is disabled";
            
            if (assignedCharacter == null)
                return "No character assigned";
            
            // TODO: Add character status check when implemented
            // if (assignedCharacter.isKnockedOut)
            //     return $"{assignedCharacter.characterName} is knocked out";
            
            if (sourceComponent.hasActedThisTurn)
                return "Already acted this turn";
            
            // Can act - no reason
            return null;
        }
    }
}
