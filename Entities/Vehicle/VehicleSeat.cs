using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets.Scripts.Entities.Vehicle
{
    /// <summary>
    /// Physical seat/station on a vehicle. Characters can only operate components
    /// reachable from their seat.
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
        public List<VehicleComponent> controlledComponents = new();

        [Header("Character Assignment")]
        [Tooltip("Character currently occupying this seat. Drag PlayerCharacter ScriptableObject here. " +
                 "Leave empty for uncrewed/AI-controlled seats.")]
        public Character assignedCharacter;

        // ==================== TURN STATE ====================
        
        [NonSerialized]
        public bool hasActedThisTurn = false;

        // ==================== ROLE QUERIES ====================

        public RoleType GetEnabledRoles()
        {
            RoleType roles = RoleType.None;

            foreach (var component in controlledComponents)
            {
                if (component != null && component.IsOperational)
                {
                    roles |= component.roleType;
                }
            }

            return roles;
        }

        public bool HasRole(RoleType role)
        {
            if (role == RoleType.None) return false;
            return (GetEnabledRoles() & role) != 0;
        }

        /// <summary>For multi-role penalties (jack of all trades).</summary>
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

        public IEnumerable<VehicleComponent> GetOperationalComponents()
        {
            return controlledComponents.Where(c => c != null && c.IsOperational);
        }

        // ==================== ACTION AVAILABILITY ====================

        public bool CanAct()
        {
            if (assignedCharacter == null)
                return false;

            return GetOperationalComponents().Any();
        }

        /// <summary>Null if can act.</summary>
        public string GetCannotActReason()
        {
            if (assignedCharacter == null)
                return "No character assigned";

            if (!GetOperationalComponents().Any())
                return "All controlled components destroyed or disabled";

            return null;
        }

        public bool HasActedThisTurn()
        {
            return hasActedThisTurn;
        }

        public void MarkAsActed()
        {
            hasActedThisTurn = true;
        }

        public void ResetTurnState()
        {
            hasActedThisTurn = false;
        }

        /// <summary>Returns null for character personal skills (not from a component).</summary>
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
    }
}
