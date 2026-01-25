using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Assets.Scripts.Combat.SkillChecks;

namespace Assets.Scripts.Entities.Vehicle
{
    /// <summary>
    /// Represents a physical position/station on a vehicle where a character sits.
    /// Defines which components are accessible from that position.
    /// 
    /// This creates spatial/narrative coherence: characters can only operate 
    /// components they can physically reach from their seat.
    /// 
    /// Examples:
    /// - Solo bike: 1 seat controlling DriveComponent + handlebar WeaponComponent
    /// - Battle wagon: 5 seats, each controlling 1 component (Driver, 2 Gunners, Engineer, Navigator)
    /// - Hybrid: 2 seats (Pilot controls Drive+ForwardGuns, Tail Gunner controls RearTurret)
    /// 
    /// Usage: Add seats to Vehicle.seats list in inspector, drag component references.
    /// </summary>
    [Serializable]
    public class VehicleSeat
    {
        [Header("Seat Identity")]
        [Tooltip("Name of this seat/station (e.g., 'Driver's Seat', 'Left Turret', 'Engineering Bay')")]
        public string seatName = "Unnamed Seat";

        [Header("Controlled Components")]
        [Tooltip("Components this seat can operate. Drag references from same vehicle. " +
                 "Character in this seat can only use skills/actions from these components.")]
        public List<VehicleComponent> controlledComponents = new List<VehicleComponent>();

        [Header("Character Assignment")]
        [Tooltip("Character currently occupying this seat. Drag PlayerCharacter ScriptableObject here. " +
                 "Leave empty for uncrewed/AI-controlled seats.")]
        public PlayerCharacter assignedCharacter;

        // ==================== TURN STATE ====================
        
        /// <summary>
        /// Has this seat (character) acted this turn?
        /// Each seat can perform one action per turn.
        /// </summary>
        [NonSerialized]
        public bool hasActedThisTurn = false;

        // ==================== ROLE QUERIES ====================

        /// <summary>
        /// Get all roles this seat enables (union of all controlled components' roles).
        /// Example: Seat controls Drive + Weapon → returns Driver | Gunner
        /// </summary>
        public RoleType GetEnabledRoles()
        {
            RoleType roles = RoleType.None;

            foreach (var component in controlledComponents)
            {
                if (component != null && !component.isDestroyed && !component.isDisabled)
                {
                    roles |= component.roleType;
                }
            }

            return roles;
        }

        /// <summary>
        /// Check if this seat enables a specific role.
        /// </summary>
        public bool HasRole(RoleType role)
        {
            if (role == RoleType.None) return false;
            return (GetEnabledRoles() & role) != 0;
        }

        /// <summary>
        /// Count how many distinct roles this seat enables.
        /// Used for multi-role penalties (jack of all trades, master of none).
        /// </summary>
        public int CountEnabledRoles()
        {
            int count = 0;
            RoleType roles = GetEnabledRoles();

            foreach (RoleType role in Enum.GetValues(typeof(RoleType)))
            {
                if (role != RoleType.None && (roles & role) == role)
                {
                    count++;
                }
            }

            return count;
        }

        // ==================== COMPONENT QUERIES ====================

        /// <summary>
        /// Get all operational (not destroyed/disabled) components this seat controls.
        /// </summary>
        public IEnumerable<VehicleComponent> GetOperationalComponents()
        {
            return controlledComponents.Where(c => c != null && !c.isDestroyed && !c.isDisabled);
        }

        /// <summary>
        /// Get the component this seat would use for a specific skill check type.
        /// Returns the most appropriate component based on role mapping.
        /// Returns null if no component can handle this check type.
        /// </summary>
        public VehicleComponent GetComponentForCheck(SkillCheckType checkType)
        {
            // Map check type to required role
            RoleType requiredRole = GetRoleForCheckType(checkType);

            // Find component with matching role
            foreach (var component in GetOperationalComponents())
            {
                if ((component.roleType & requiredRole) != 0)
                {
                    return component;
                }
            }

            // Fallback: Return first operational component (better than nothing)
            return GetOperationalComponents().FirstOrDefault();
        }

        /// <summary>
        /// Get a weapon component for attacking.
        /// Returns first operational weapon, or null if no weapons.
        /// </summary>
        public WeaponComponent GetWeaponForAttack()
        {
            return controlledComponents
                .OfType<WeaponComponent>()
                .FirstOrDefault(w => !w.isDestroyed && !w.isDisabled);
        }

        /// <summary>
        /// Get all weapon components this seat controls (for multi-weapon seats).
        /// </summary>
        public IEnumerable<WeaponComponent> GetAllWeapons()
        {
            return controlledComponents
                .OfType<WeaponComponent>()
                .Where(w => !w.isDestroyed && !w.isDisabled);
        }

        /// <summary>
        /// Get the drive component if this seat controls one.
        /// </summary>
        public DriveComponent GetDriveComponent()
        {
            return controlledComponents
                .OfType<DriveComponent>()
                .FirstOrDefault(d => !d.isDestroyed && !d.isDisabled);
        }

        // ==================== ACTION AVAILABILITY ====================

        /// <summary>
        /// Check if this seat can currently act.
        /// Requires: character assigned AND at least one operational component.
        /// </summary>
        public bool CanAct()
        {
            // Must have a character
            if (assignedCharacter == null)
                return false;

            // Must have at least one operational component
            return GetOperationalComponents().Any();
        }

        /// <summary>
        /// Get the reason why this seat cannot act (for UI/debugging).
        /// Returns null if seat can act.
        /// </summary>
        public string GetCannotActReason()
        {
            if (assignedCharacter == null)
                return "No character assigned";

            if (!GetOperationalComponents().Any())
                return "All controlled components destroyed or disabled";

            return null; // Can act
        }

        /// <summary>
        /// Check if this seat has acted this turn.
        /// </summary>
        public bool HasActedThisTurn()
        {
            return hasActedThisTurn;
        }
        
        /// <summary>
        /// Mark this seat as having acted this turn.
        /// </summary>
        public void MarkAsActed()
        {
            hasActedThisTurn = true;
        }

        /// <summary>
        /// Reset turn state for this seat.
        /// Called at start of each round.
        /// </summary>
        public void ResetTurnState()
        {
            hasActedThisTurn = false;
        }
        
        /// <summary>
        /// Get the component that provides a specific skill, or null if it's a character personal skill.
        /// Used to determine SourceEntity for SkillContext construction.
        /// </summary>
        public VehicleComponent GetComponentForSkill(Skill skill)
        {
            if (skill == null) return null;
            
            // Check each operational component
            foreach (var component in GetOperationalComponents())
            {
                if (component.GetAllSkills().Contains(skill))
                {
                    return component;
                }
            }
            
            // Not from a component - must be character personal skill (or not found)
            return null;
        }

        // ==================== HELPERS ====================

        /// <summary>
        /// Map skill check type to the role that handles it.
        /// Extend this as more SkillCheckTypes are added.
        /// </summary>
        private static RoleType GetRoleForCheckType(SkillCheckType checkType)
        {
            return checkType switch
            {
                SkillCheckType.Mobility => RoleType.Driver,
                // Future mappings (add when SkillCheckType expands):
                // SkillCheckType.Perception => RoleType.Navigator,
                // SkillCheckType.Mechanics => RoleType.Technician,
                // SkillCheckType.Gunnery => RoleType.Gunner,
                _ => RoleType.None
            };
        }
    }
}
