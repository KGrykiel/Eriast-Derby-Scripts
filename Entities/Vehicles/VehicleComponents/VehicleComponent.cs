using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;
using Assets.Scripts.Core;
using Assets.Scripts.Entities.Vehicles.VehicleComponents.ComponentTypes;
using Assets.Scripts.Modifiers;

namespace Assets.Scripts.Entities.Vehicles.VehicleComponents
{
    /// <summary>
    /// Base class for vehicle components. Components ARE Entities (have HP, can be damaged).
    /// Main source of interaction between vehicles.
    /// Components can provide modifiers to other components via providedModifiers.
    /// </summary>
    public abstract class VehicleComponent : Entity, IModifierSource
    {
        public string ModifierLabel => name;

        [Header("Component Identity")]
        [Tooltip("Category of this component (locked for specific component types)")]
        public ComponentType componentType = ComponentType.Custom;

        [Header("Component-Specific Stats")]
        [SerializeField]
        [Tooltip("Component Space (positive = uses space, negative = provides space) (base value before modifiers)")]
        protected int baseComponentSpace = 0;

        [SerializeField]
        [Tooltip("Power drawn per turn (0 = passive component, no continuous draw) (base value before modifiers)")]
        protected int basePowerDrawPerTurn = 0;

        [Header("Provided Modifiers")]
        [Tooltip("Modifiers this component provides to OTHER components. Used for cross-component bonuses like armor upgrades, boosters, etc.")]
        public List<ComponentModifierData> providedModifiers = new();

        [Header("Component Targeting")]
        [Tooltip("How exposed this component is for targeting")]
        public ComponentExposure exposure = ComponentExposure.External;

        [Tooltip("Component that shields this one (drag component reference here)")]
        public VehicleComponent shieldedByComponent = null;

        [Tooltip("For Internal exposure: Required chassis damage % to access (e.g., 50 = 50% damage)")]
        [Range(0, 100)]
        public int internalAccessThreshold = 50;

        [Header("Component State")]
        [Tooltip("Has the engineer manually disabled this component? (Does not draw power, cannot use skills, does not provide bonuses)")]
        public bool isManuallyDisabled = false;

        [Header("Role Support")]
        [Tooltip("Type of role this component enables. Set to None if component doesn't enable a role.")]
        public RoleType roleType = RoleType.None;

        [Header("Skills")]
        [Tooltip("Skills provided by this component (assigned in Inspector)")]
        public List<Skill> componentSkills = new();

        [Header("Advantage / Disadvantage")]
        [Tooltip("Advantage/disadvantage grants this component provides. Use targetMode to control which component's rolls are affected.")]
        public List<ComponentAdvantageGrantData> providedAdvantageGrants = new();

        // Reference to parent vehicle (set during initialization)
        protected Vehicle parentVehicle;

        public Vehicle ParentVehicle => parentVehicle;

        // ==================== INITIALIZATION ====================

        public virtual void Initialize(Vehicle vehicle)
        {
            parentVehicle = vehicle;

            // Log component initialization
            if (roleType != RoleType.None)
            {
                Debug.Log($"[Component] {name} initialized on {vehicle.vehicleName}, enables role: {roleType}");
            }
        }

        // ==================== STAT ACCESSORS ====================

        public int GetBaseComponentSpace() => baseComponentSpace;
        public int GetBasePowerDrawPerTurn() => basePowerDrawPerTurn;

        public virtual int GetComponentSpace() => StatCalculator.GatherAttributeValue(this, EntityAttribute.ComponentSpace);
        public virtual int GetPowerDrawPerTurn() => StatCalculator.GatherAttributeValue(this, EntityAttribute.PowerDraw);

        public override int GetBaseValue(EntityAttribute attribute)
        {
            return attribute switch
            {
                EntityAttribute.ComponentSpace => baseComponentSpace,
                EntityAttribute.PowerDraw => basePowerDrawPerTurn,
                _ => base.GetBaseValue(attribute)
            };
        }

        // ==================== CROSS-COMPONENT MODIFIER SYSTEM ====================

        /// <summary>
        /// If the component provides modifiers to other components, they are applied at startup.
        /// </summary>
        public virtual void ApplyProvidedModifiers(Vehicle vehicle)
        {
            if (!IsOperational) return;
            if (vehicle == null) return;

            foreach (var modData in providedModifiers)
            {
                var targets = ResolveTargets(vehicle, modData.targetMode, modData.specificTarget);
                foreach (var target in targets)
                {
                    if (target != null && !target.IsDestroyed())
                    {
                        modData.modifier.Source = this;
                        target.AddModifier(modData.modifier);
                    }
                }
            }
        }

        public virtual void RemoveProvidedModifiers(Vehicle vehicle)
        {
            if (vehicle == null) return;

            foreach (var component in vehicle.AllComponents)
                component.RemoveModifiersFromSource(this);
        }

        public virtual void ApplyProvidedAdvantageGrants(Vehicle vehicle)
        {
            if (!IsOperational) return;
            if (vehicle == null) return;

            foreach (var grantData in providedAdvantageGrants)
            {
                var targets = ResolveTargets(vehicle, grantData.targetMode, grantData.specificTarget);
                foreach (var target in targets)
                {
                    if (target != null && !target.IsDestroyed())
                    {
                        grantData.grant.Source = this;
                        target.AddAdvantageGrant(grantData.grant);
                    }
                }
            }
        }

        public virtual void RemoveProvidedAdvantageGrants(Vehicle vehicle)
        {
            if (vehicle == null) return;

            foreach (var component in vehicle.AllComponents)
                component.RemoveAdvantageGrantsFromSource(this);
        }

        private List<VehicleComponent> ResolveTargets(Vehicle vehicle, ComponentTargetMode targetMode, VehicleComponent specificTarget)
        {
            var targets = new List<VehicleComponent>();

            switch (targetMode)
            {
                case ComponentTargetMode.Chassis:
                    if (vehicle.chassis != null)
                        targets.Add(vehicle.chassis);
                    break;

                case ComponentTargetMode.PowerCore:
                    if (vehicle.powerCore != null)
                        targets.Add(vehicle.powerCore);
                    break;

                case ComponentTargetMode.Drive:
                    var drive = vehicle.AllComponents.OfType<DriveComponent>().FirstOrDefault();
                    if (drive != null)
                        targets.Add(drive);
                    break;

                case ComponentTargetMode.AllWeapons:
                    foreach (var weapon in vehicle.AllComponents.OfType<WeaponComponent>())
                    {
                        targets.Add(weapon);
                    }
                    break;

                case ComponentTargetMode.AllComponents:
                    targets.AddRange(vehicle.AllComponents);
                    break;

                case ComponentTargetMode.SpecificComponent:
                    if (specificTarget != null)
                        targets.Add(specificTarget);
                    break;

                case ComponentTargetMode.Self:
                    targets.Add(this);
                    break;
            }

            return targets;
        }

        // ==================== MODIFIER SYSTEM

        /// <summary>
        /// override to add logging.
        /// </summary>
        public override void RemoveModifier(EntityAttributeModifier modifier)
        {
            base.RemoveModifier(modifier);
            this.LogModifierRemoved(modifier);
        }

        // ==================== ENTITY OVERRIDES ====================

        public override bool CanBeTargeted()
        {
            if (IsDestroyed()) return false;

            if (parentVehicle != null)
                return parentVehicle.IsComponentAccessible(this);

            return true;
        }

        // ==================== BEHAVIORAL QUERIES ====================

        public virtual bool IsOperational
        {
            get
            {
                if (IsDestroyed() || isManuallyDisabled) return false;

                foreach (var statusEffect in GetActiveConditions())
                {
                    if (statusEffect.PreventsActions)
                        return false;
                }

                // Vehicle-wide stuns are applied to chassis, so check those too
                if (parentVehicle != null && parentVehicle.chassis != this && parentVehicle.IsChassisStunned)
                    return false;

                return true;
            }
        }

        public virtual bool CanContributeToMovement()
        {
            if (!IsOperational) return false;

            foreach (var statusEffect in GetActiveConditions())
            {
                if (statusEffect.PreventsMovement)
                    return false;
            }

            return true;
        }

        // ==================== DAMAGE HANDLING (Override Entity) ====================

        protected override void OnEntityDestroyed()
        {
            this.LogComponentDestroyed();

            OnComponentDestroyed();
        }

        protected virtual void OnComponentDestroyed()
        {
            RemoveProvidedModifiers(parentVehicle);
            RemoveProvidedAdvantageGrants(parentVehicle);
        }

        protected virtual void OnComponentDisabled()
        {
            RemoveProvidedModifiers(parentVehicle);
            RemoveProvidedAdvantageGrants(parentVehicle);
        }

        protected virtual void OnComponentEnabled()
        {
            ApplyProvidedModifiers(parentVehicle);
            ApplyProvidedAdvantageGrants(parentVehicle);
        }

        // ==================== SKILL MANAGEMENT ====================

        public virtual List<Skill> GetAllSkills()
        {
            List<Skill> allSkills = new();

            if (componentSkills != null)
                allSkills.AddRange(componentSkills);

            return allSkills;
        }

        // ==================== UI HELPERS ====================

        public virtual List<VehicleComponentUI.DisplayStat> GetDisplayStats()
        {
            var stats = new List<VehicleComponentUI.DisplayStat>();

            if (basePowerDrawPerTurn > 0)
            {
                int modifiedPower = GetPowerDrawPerTurn();
                stats.Add(VehicleComponentUI.DisplayStat.WithTooltip("Power", "PWR", EntityAttribute.PowerDraw, basePowerDrawPerTurn, modifiedPower, "/turn"));
            }

            return stats;
        }

        // ==================== POWER MANAGEMENT ====================

        public virtual bool DrawTurnPower()
        {
            if (!IsOperational) return true;

            var powerCore = parentVehicle != null ? parentVehicle.powerCore : null;
            if (powerCore == null) return true;

            int actualDraw = GetActualPowerDraw();
            if (actualDraw <= 0) return true;

            bool success = powerCore.DrawPower(actualDraw, this, "Continuous operation");

            if (!success)
                OnPowerStarved();

            return success;
        }

        public virtual int GetActualPowerDraw()
        {
            if (!IsOperational) return 0;
            return GetPowerDrawPerTurn();
        }

        protected virtual void OnPowerStarved()
        {
            this.LogPowerStarved(GetActualPowerDraw());
        }

        /// <summary>Returns false if destroyed or trying to disable mandatory components.</summary>
        public virtual bool SetManuallyDisabled(bool disabled)
        {
            if (IsDestroyed()) return false;

            if (disabled && (componentType == ComponentType.Chassis || componentType == ComponentType.PowerCore))
                return false;

            bool oldState = isManuallyDisabled;
            isManuallyDisabled = disabled;

            if (oldState != isManuallyDisabled)
            {
                if (disabled)
                {
                    OnComponentDisabled();
                }
                else
                {
                    OnComponentEnabled();
                }

                this.LogManualStateChange(isManuallyDisabled);
            }

            return true;
        }
    }
}