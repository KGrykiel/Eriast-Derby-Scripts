using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Assets.Scripts.Characters;
using Assets.Scripts.Combat.Rolls.Advantage;
using Assets.Scripts.Conditions;
using Assets.Scripts.Conditions.CharacterConditions;
using Assets.Scripts.Consumables;
using Assets.Scripts.Effects;
using Assets.Scripts.Entities.Vehicles.VehicleComponents;
using Assets.Scripts.Modifiers;
using Assets.Scripts.Skills;

namespace Assets.Scripts.Entities.Vehicles
{
    /// <summary>
    /// Physical seat/station on a vehicle. Characters can only operate components
    /// reachable from their seat.
    /// </summary>
    [Serializable]
    public class VehicleSeat : IRollTarget, IEffectTarget
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
        [SerializeField] private Character assignedCharacter;

        [Header("Consumable Access")]
        [Tooltip("Which consumable categories this seat's occupant can physically use.")]
        public ConsumableAccess consumableAccess = ConsumableAccess.None;

        // ==================== VEHICLE REFERENCE ====================

        [NonSerialized]
        public Vehicle ParentVehicle;

        // ==================== TURN STATE ====================

        [NonSerialized]
        private readonly Dictionary<ActionType, int> _actionPool = new();

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

            if (HasConditionPreventingActions())
                return false;

            return GetOperationalComponents().Any();
        }

        /// <summary>Null if can act.</summary>
        public string GetCannotActReason()
        {
            if (assignedCharacter == null)
                return "No character assigned";

            if (HasConditionPreventingActions())
                return "Character is incapacitated";

            if (!GetOperationalComponents().Any())
                return "All controlled components destroyed or disabled";

            return null;
        }

        public bool CanSpendAction(ActionType type)
        {
            if (type == ActionType.Free) return true;
            if (type == ActionType.FullAction)
                return CanSpendAction(ActionType.Action) && CanSpendAction(ActionType.BonusAction);
            if (!_actionPool.ContainsKey(type)) return true;
            return _actionPool[type] > 0;
        }

        public void SpendAction(ActionType type)
        {
            if (type == ActionType.Free) return;
            if (type == ActionType.FullAction)
            {
                SpendAction(ActionType.Action);
                SpendAction(ActionType.BonusAction);
                return;
            }
            if (_actionPool.ContainsKey(type) && _actionPool[type] > 0)
                _actionPool[type]--;
        }

        public void GrantExtraAction(ActionType type, int count = 1)
        {
            if (type == ActionType.Free || type == ActionType.FullAction) return;
            _actionPool.TryGetValue(type, out int current);
            _actionPool[type] = current + count;
        }

        public bool HasAnyActionsRemaining()
        {
            return CanSpendAction(ActionType.Action) || CanSpendAction(ActionType.BonusAction);
        }

        public int GetActionCount(ActionType type)
        {
            if (type == ActionType.Free || type == ActionType.FullAction) return 0;
            _actionPool.TryGetValue(type, out int count);
            return count;
        }

        public void ResetTurnState()
        {
            _actionPool[ActionType.Action] = 1;
            _actionPool[ActionType.BonusAction] = 1;
        }

        public void NotifyConditionTrigger(RemovalTrigger trigger)
        {
            ConditionManager.ProcessRemovalTrigger(trigger);
        }

        // ==================== CONDITION MANAGEMENT ====================

        [NonSerialized]
        private CharacterConditionManager _conditionManager;

        [NonSerialized]
        private readonly List<CharacterModifier> _characterModifiers = new();

        [NonSerialized]
        private readonly List<AdvantageGrant> _advantageGrants = new();

        private CharacterConditionManager ConditionManager
        {
            get
            {
                if (_conditionManager == null)
                    _conditionManager = new CharacterConditionManager(this);
                return _conditionManager;
            }
        }

        public AppliedCharacterCondition ApplyCondition(CharacterCondition condition, UnityEngine.Object applier)
            => ConditionManager.Apply(condition, applier);

        public void RemoveConditionsByTemplate(CharacterCondition template)
            => ConditionManager.RemoveByTemplate(template);

        public void RemoveConditionsByCategory(ConditionCategory categories)
            => ConditionManager.RemoveByCategory(categories);

        public List<AppliedCharacterCondition> GetActiveConditions()
            => ConditionManager.GetActive();

        public void UpdateConditions()
            => ConditionManager.OnTurnStart();

        public bool HasConditionPreventingActions()
            => ConditionManager.GetActive().Any(c => c.PreventsActions);

        public void AddCharacterModifier(CharacterModifier modifier) => _characterModifiers.Add(modifier);
        public void RemoveCharacterModifier(CharacterModifier modifier) => _characterModifiers.Remove(modifier);
        public void RemoveCharacterModifiersFromSource(object source) => _characterModifiers.RemoveAll(m => m.Source == source);
        public List<CharacterModifier> GetCharacterModifiers() => _characterModifiers;

        public void AddAdvantageGrant(AdvantageGrant grant) => _advantageGrants.Add(grant);
        public void RemoveAdvantageGrantsFromSource(object source) => _advantageGrants.RemoveAll(g => g.Source == source);
        public IReadOnlyList<AdvantageGrant> GetAdvantageGrants() => _advantageGrants;

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

        // ==================== CHARACTER DATA ====================

        public void Assign(Character character) { assignedCharacter = character; }

        public bool IsAssigned => assignedCharacter != null;

        public bool IsAssignedTo(Character character) => assignedCharacter == character;

        public string GetDisplayName() => assignedCharacter != null ? assignedCharacter.characterName : null;

        public List<Skill> GetPersonalAbilities()
        {
            if (assignedCharacter == null) return new List<Skill>();
            return assignedCharacter.GetPersonalAbilities();
        }

        public int GetAttributeScore(CharacterAttribute attribute)
        {
            if (assignedCharacter == null) return 0;
            return assignedCharacter.GetAttributeScore(attribute);
        }

        public bool IsProficientIn(CharacterSkill skill)
        {
            return assignedCharacter != null && assignedCharacter.IsProficient(skill);
        }

        public int GetLevel()
        {
            if (assignedCharacter == null) return 0;
            return assignedCharacter.level;
        }

        public int GetBaseAttackBonus()
        {
            if (assignedCharacter == null) return 0;
            return assignedCharacter.baseAttackBonus;
        }
    }
}
