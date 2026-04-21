using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Items;
using Assets.Scripts.Entities.Vehicles.VehicleComponents.ComponentTypes;
using Assets.Scripts.Conditions;
using Assets.Scripts.Combat.Rolls.RollSpecs;
using Assets.Scripts.Effects;
using Assets.Scripts.Entities.Vehicles.VehicleComponents;
using Assets.Scripts.Skills.Helpers;
using Assets.Scripts.Characters;
using Assets.Scripts.Skills;
using Assets.Scripts.Conditions.VehicleConditions;
using Assets.Scripts.Combat.Restoration;
using Assets.Scripts.Managers.Turn;
using Assets.Scripts.Managers.Race;

namespace Assets.Scripts.Entities.Vehicles
{
    /// <summary>
    /// Container/coordinator for Entity components. NOT an Entity itself.
    /// Damage to "the vehicle" is actually damage to its chassis.
    /// Vehicle can be targeted directly — each effect's Apply handles internal routing.
    /// </summary>
    public class Vehicle : MonoBehaviour, IRollTarget, IEffectTarget
    {
        [Header("Vehicle Identity")]
        public string vehicleName;

        public ControlType controlType = ControlType.Player;

        [Tooltip("Vehicles sharing the same team asset are allies. Leave null for independent vehicles.")]
        public VehicleTeam team;

        public bool HasMovedThisTurn { get; private set; }

        [Header("Crew & Seats")]
        [Tooltip("Physical positions where characters sit and control components. " +
                 "Each seat references components it can operate and has an assigned character.")]
        public List<VehicleSeat> seats = new();

        [Header("Inventory")]
        [Tooltip("Consumable stacks the vehicle starts with. Total bulk must not exceed the chassis cargo capacity.")]
        public List<ItemStack> inventory = new();

        [Header("Vehicle Components")]
        public ChassisComponent Chassis => componentCoordinator?.Chassis;
        public PowerCoreComponent PowerCore => componentCoordinator?.PowerCore;
        public DriveComponent Drive => componentCoordinator?.Drive;

        // Coordinators (handle distinct concerns)
        private VehicleComponentCoordinator componentCoordinator;
        private VehicleInventoryCoordinator inventoryCoordinator;
        private VehicleConditionManager conditionManager;

        public VehicleStatus Status { get; private set; } = VehicleStatus.Active;

        void Awake()
        {
            componentCoordinator = new VehicleComponentCoordinator(this);

            componentCoordinator.InitializeComponents();

            componentCoordinator.ApplySizeModifiers();

            foreach (var seat in seats)
                seat.ParentVehicle = this;

            inventoryCoordinator = new VehicleInventoryCoordinator(this);
            conditionManager = new VehicleConditionManager(this);
        }

        void OnEnable()
        {
            RacePositionTracker.Register(this);
        }

        void OnValidate()
        {
            // Validate component space usage
            if (Chassis == null) return;

            int netSpace = componentCoordinator?.CalculateNetComponentSpace() ?? 0;

            if (netSpace > 0)
            {
                Debug.LogError($"[Vehicle] {vehicleName} exceeds component space by {netSpace} units! " +
                              $"Remove components or upgrade chassis.");
            }
        }


        // ==================== COMPONENT ACCESS (delegate to coordinator) ====================

        public IReadOnlyList<VehicleComponent> AllComponents => componentCoordinator?.GetAllComponents() ?? System.Array.Empty<VehicleComponent>();

        public void RegisterComponent(VehicleComponent component) => componentCoordinator?.RegisterComponent(component);

        public bool IsComponentAccessible(VehicleComponent target) => componentCoordinator?.IsComponentAccessible(target) ?? false;

        public string GetInaccessibilityReason(VehicleComponent target) => componentCoordinator?.GetInaccessibilityReason(target);

        public VehicleComponent GetComponentOfRole(RoleType role)
            => AllComponents.FirstOrDefault(c => c != null && (c.roleType & role) != 0);

        public void ResetComponentsForNewTurn()
        {
            HasMovedThisTurn = false;

            // Reset seat turn state (seats track action usage now)
            foreach (var seat in seats)
            {
                seat?.ResetTurnState();
            }
        }

        /// <summary>Marks the vehicle as having moved this turn.</summary>
        public void MarkMoved() => HasMovedThisTurn = true;

        /// <summary>Sets the target speed percentage on the drive component. No-op with a warning if no drive is present.</summary>
        public void SetTargetSpeed(int speedPercent)
        {
            var drive = Drive;
            if (drive == null)
            {
                Debug.LogWarning($"[Vehicle] '{name}' has no drive component — SetTargetSpeed had no effect.");
                return;
            }

            drive.SetTargetSpeed(speedPercent);
        }


        /// <summary>Adjusts the drive component's speed toward its target, or applies friction if inoperational.</summary>
        public void Accelerate()
        {
            var drive = Drive;
            if (drive == null) return;

            if (!drive.IsOperational)
            {
                drive.ApplyFriction();
                return;
            }

            drive.AdjustSpeedTowardTarget();
        }

        /// <summary>
        /// Draws continuous turn power for all components.
        /// Components that cannot be powered are shut down and an event is emitted.
        /// </summary>
        public void DrawTurnPower()
        {
            if (PowerCore == null) return;

            foreach (var component in AllComponents)
            {
                if (component == null) continue;

                bool success = component.DrawTurnPower();

                if (!success)
                {
                    TurnEventBus.Emit(new ComponentPowerShutdownEvent(
                        this,
                        component,
                        component.GetActualPowerDraw(),
                        PowerCore.currentEnergy));
                    //TODO: should probably add logic to determine which components get priority power instead of just shutting down everything that can't be powered
                    component.SetManuallyDisabled(true);
                }
            }
        }

        // ==================== SEAT ACCESS ====================

        public VehicleSeat GetSeatForComponent(VehicleComponent component)
        {
            if (component == null) return null;
            return seats.FirstOrDefault(s => s.controlledComponents.Contains(component));
        }

        public VehicleSeat GetSeatForCharacter(Character character)
        {
            if (character == null) return null;
            return seats.FirstOrDefault(s => s != null && s.IsAssignedTo(character));
        }

        public List<VehicleSeat> GetActiveSeats()
        {
            return seats.Where(s => s != null && s.CanAct()).ToList();
        }

        // ==================== CONSUMABLE INVENTORY ====================

        public IReadOnlyList<ItemStack> GetConsumables() => inventoryCoordinator.GetConsumables();

        public IReadOnlyList<ItemStack> GetAvailableConsumables(VehicleSeat seat) => inventoryCoordinator.GetAvailableConsumables(seat);

        public bool HasChargesFor(ItemBase template) => inventoryCoordinator.HasChargesFor(template);

        public bool TrySpendConsumable(ItemBase template, string causalSource = "") => inventoryCoordinator.TrySpendConsumable(template, causalSource);

        public void RestoreConsumable(ItemBase template, int amount, string causalSource = "") => inventoryCoordinator.RestoreConsumable(template, amount, causalSource);

        public void TrimInventoryToCapacity() => inventoryCoordinator.TrimInventoryToCapacity();

        // ==================== STATE QUERIES ====================

        /// <summary>Live vehicle state values that can be queried for threshold checks.</summary>
        public enum RuntimeState
        {
            CurrentSpeed,
            CurrentEnergy,
            CurrentHealth,
            CurrentProgress,
        }

        /// <summary>Returns the current value of a live vehicle state. Used by StateThresholdSpec.</summary>
        public int GetStateValue(RuntimeState state)
        {
            var drive = Drive;
            return state switch
            {
                RuntimeState.CurrentSpeed => drive != null ? drive.GetCurrentSpeed() : 0,
                RuntimeState.CurrentEnergy => PowerCore != null ? PowerCore.GetCurrentEnergy() : 0,
                RuntimeState.CurrentHealth => Chassis != null ? Chassis.GetCurrentHealth() : 0,
                RuntimeState.CurrentProgress => RacePositionTracker.GetProgress(this),
                _ => 0
            };
        }

        // ==================== EFFECT ROUTING ====================

        /// <summary>Applies a vehicle-wide condition tracked by VehicleConditionManager.</summary>
        public void ApplyVehicleCondition(VehicleCondition condition, Object applier)
            => conditionManager.Apply(condition, applier);

        /// <summary>Removes vehicle-wide conditions matching a specific template.</summary>
        public void RemoveVehicleConditionsByTemplate(VehicleCondition template)
            => conditionManager.RemoveByTemplate(template);

        /// <summary>Removes vehicle-wide conditions matching the given categories.</summary>
        public void RemoveVehicleConditionsByCategory(ConditionCategory categories)
            => conditionManager.RemoveByCategory(categories);

        /// <summary>Returns all currently active vehicle-wide conditions.</summary>
        public IReadOnlyList<AppliedVehicleCondition> GetActiveVehicleConditions()
            => conditionManager.GetActive();

        /// <summary>
        /// Returns the default component to target for a vehicle-level resource effect
        /// when no specific component has been selected.
        /// Energy routes exclusively to the power core (the sole energy holder).
        /// Health defaults to the chassis as the vehicle's structural representative —
        /// all components have independent health pools; chassis is only the fallback target.
        /// </summary>
        public Entity GetDefaultTargetForResource(ResourceType resourceType)
        {
            if (resourceType == ResourceType.Energy && PowerCore != null)
                return PowerCore;
            return Chassis;
        }

        /// <summary>
        /// Adds a permanent modifier to the component that owns the modifier's attribute.
        /// </summary>
        public void AddModifier(Modifiers.EntityAttributeModifier modifier)
        {
            VehicleComponent component = VehicleComponentResolver.ResolveForAttribute(this, modifier.Attribute);
            if (component == null)
                component = Chassis;
            if (component != null)
                component.AddModifier(modifier);
        }

        public void UpdateStatusEffects()
        {
            conditionManager.OnTurnStart();

            foreach (var component in AllComponents)
            {
                component.UpdateConditions();
            }

            foreach (var seat in seats)
            {
                seat.UpdateConditions();
            }
        }

        /// <summary>Broadcasts a removal trigger to the vehicle condition manager, all components, and all seats.</summary>
        public void NotifyStatusEffectTrigger(RemovalTrigger trigger)
        {
            conditionManager.ProcessRemovalTrigger(trigger);

            foreach (var component in AllComponents)
            {
                component.NotifyConditionTrigger(trigger);
            }

            foreach (var seat in seats)
            {
                seat.NotifyConditionTrigger(trigger);
            }
        }

        // ==================== VEHICLE STATUS ====================

        public void MarkAsDestroyed()
        {
            if (Status == VehicleStatus.Destroyed) return; // Already handled

            Status = VehicleStatus.Destroyed;

            // Emit event - TurnStateMachine removes from turn order, GameManager checks game over
            TurnEventBus.Emit(new VehicleDestroyedEvent(this));
        }

        public void MarkAsFinished()
        {
            if (Status == VehicleStatus.Finished) return; // Already handled

            Status = VehicleStatus.Finished;

            // Emit event - GameManager records ranking, TurnStateMachine skips future turns
            TurnEventBus.Emit(new VehicleFinishedEvent(this));
        }

        // ==================== OPERATIONAL STATUS ====================

        /// <summary>Null if operational.</summary>
        public string GetNonOperationalReason()
        {
            if (Chassis == null) return "No chassis installed";
            if (Chassis.IsDestroyed()) return "Chassis destroyed";
            if (PowerCore == null) return "No power core installed";
            if (PowerCore.IsDestroyed()) return "Power core destroyed - no power";
            return null;
        }

        public bool IsOperational() => GetNonOperationalReason() == null;

        /// <summary>True if any chassis condition is blocking all component actions vehicle-wide.</summary>
        public bool IsChassisStunned
        {
            get
            {
                if (Chassis == null) return false;
                foreach (var effect in Chassis.GetActiveConditions())
                {
                    if (effect.PreventsActions) return true;
                }
                return false;
            }
        }

        /// <summary>Null if can move.</summary>
        public string GetCannotMoveReason()
        {
            // Check operational status first
            string reason = GetNonOperationalReason();
            if (reason != null) return reason;

            // Check drive system
            var driveComponent = Drive;
            if (driveComponent == null) return "No drive system installed";
            if (driveComponent.IsDestroyed()) return "Drive system destroyed";
            if (driveComponent.isManuallyDisabled) return "Drive system manually disabled by engineer";
            if (!driveComponent.CanContributeToMovement()) return "Drive system immobilized by status effect";

            return null;
        }

        public bool CanMove() => GetCannotMoveReason() == null;

        // ==================== SKILL EXECUTION ====================

        public bool ExecuteSkill(RollContext ctx, Skill skill)
        {
            if (ctx.Target == null)
            {
                Debug.LogError($"[Vehicle] ExecuteSkill called with null target!");
                return false;
            }

            // Skill validation (costs, configuration, and targeting)
            if (!SkillValidator.Validate(ctx, skill))
                return false;

            // Commit: pay all costs
            VehicleComponent sourceComponent = ctx.SourceActor?.GetEntity() as VehicleComponent;
            foreach (var cost in skill.costs)
                cost.Pay(this, ctx);

            // Fire OnSkillUsed removal trigger — once per committed skill use
            if (sourceComponent != null)
                sourceComponent.NotifyConditionTrigger(RemovalTrigger.OnSkillUsed);

            // Spend action — committed to the attempt regardless of roll outcome
            ctx.SourceActor.GetSeat().SpendAction(skill.actionCost);

            // Execute via RollNodeExecutor
            bool result = RollNodeExecutor.Execute(skill.rollNode, ctx);

            // Notify source component of success (e.g. WeaponComponent applies ammunition on hit)
            if (result && sourceComponent != null)
                sourceComponent.OnSkillSucceeded(ctx, skill);

            return result;
        }
    }
}