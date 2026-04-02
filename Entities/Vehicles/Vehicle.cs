using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Consumables;
using Assets.Scripts.Entities.Vehicles.VehicleComponents.ComponentTypes;
using Assets.Scripts.Managers;
using Assets.Scripts.Stages;
using Assets.Scripts.Stages.Lanes;
using Assets.Scripts.Conditions;
using RollContext = Assets.Scripts.Combat.Rolls.RollSpecs.RollContext;
using Assets.Scripts.Combat.Rolls.RollSpecs;
using Assets.Scripts.Effects;
using Assets.Scripts.Entities.Vehicles.VehicleComponents;
using Assets.Scripts.Skills.Helpers;
using Assets.Scripts.Characters;
using Assets.Scripts.Skills;

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

        [HideInInspector] public Stage currentStage;
        [HideInInspector] public Stage previousStage;
        [HideInInspector] public int progress = 0;
        [HideInInspector] public bool hasMovedThisTurn = false;
        [HideInInspector] public bool hasLoggedMovementWarningThisTurn = false;
        [HideInInspector] public StageLane currentLane;

        [Header("Crew & Seats")]
        [Tooltip("Physical positions where characters sit and control components. " +
                 "Each seat references components it can operate and has an assigned character.")]
        public List<VehicleSeat> seats = new();

        [Header("Inventory")]
        [Tooltip("Consumable stacks the vehicle starts with. Total bulk must not exceed the chassis cargo capacity.")]
        public List<ConsumableStack> inventory = new();

        [Header("Vehicle Components")]
        public ChassisComponent chassis => componentCoordinator?.Chassis;
        public PowerCoreComponent powerCore => componentCoordinator?.PowerCore;

        // Coordinators (handle distinct concerns)
        private VehicleComponentCoordinator componentCoordinator;
        private VehicleInventoryCoordinator inventoryCoordinator;

        public VehicleStatus Status { get; private set; } = VehicleStatus.Active;

        void Awake()
        {
            componentCoordinator = new VehicleComponentCoordinator(this);

            componentCoordinator.InitializeComponents();

            componentCoordinator.ApplySizeModifiers();

            foreach (var seat in seats)
                seat.ParentVehicle = this;

            inventoryCoordinator = new VehicleInventoryCoordinator(this);
        }

        void OnValidate()
        {
            // Validate component space usage
            if (chassis == null) return;

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

        public DriveComponent GetDriveComponent()
            => AllComponents.OfType<DriveComponent>().FirstOrDefault();

        public VehicleComponent GetComponentOfType(ComponentType type)
            => AllComponents.FirstOrDefault(c => c.componentType == type);

        public void ResetComponentsForNewTurn()
        {
            hasMovedThisTurn = false;
            hasLoggedMovementWarningThisTurn = false;

            // Reset seat turn state (seats track action usage now)
            foreach (var seat in seats)
            {
                seat?.ResetTurnState();
            }
        }

        /// <summary>Applies movement distance to progress and marks the vehicle as having moved.</summary>
        public void ApplyMovement(int distance)
        {
            if (distance > 0 && currentStage != null)
            {
                progress += distance;
            }

            hasMovedThisTurn = true;
        }

        /// <summary>Transitions vehicle to a new stage. Carries over excess progress.</summary>
        public void TransitionToStage(Stage newStage)
        {
            Stage oldStage = currentStage;

            progress -= oldStage != null ? oldStage.length : 0;
            currentStage = newStage;

            Vector3 stagePos = newStage.transform.position;
            transform.position = new Vector3(stagePos.x, stagePos.y, transform.position.z);
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

        public IReadOnlyList<ConsumableStack> GetConsumables() => inventoryCoordinator.GetConsumables();

        public IReadOnlyList<ConsumableStack> GetAvailableConsumables(VehicleSeat seat) => inventoryCoordinator.GetAvailableConsumables(seat);

        public bool HasChargesFor(ConsumableBase template) => inventoryCoordinator.HasChargesFor(template);

        public bool TrySpendConsumable(ConsumableBase template, string causalSource = "") => inventoryCoordinator.TrySpendConsumable(template, causalSource);

        public void RestoreConsumable(ConsumableBase template, int amount, string causalSource = "") => inventoryCoordinator.RestoreConsumable(template, amount, causalSource);

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
            var drive = GetDriveComponent();
            return state switch
            {
                RuntimeState.CurrentSpeed => drive != null ? drive.GetCurrentSpeed() : 0,
                RuntimeState.CurrentEnergy => powerCore != null ? powerCore.GetCurrentEnergy() : 0,
                RuntimeState.CurrentHealth => chassis != null ? chassis.GetCurrentHealth() : 0,
                RuntimeState.CurrentProgress => progress,
                _ => 0
            };
        }

        // ==================== EFFECT ROUTING ====================

        public void UpdateStatusEffects()
        {
            foreach (var component in AllComponents)
            {
                component.UpdateConditions();
            }

            foreach (var seat in seats)
            {
                seat.UpdateConditions();
            }
        }

        /// <summary>Broadcasts a removal trigger to all vehicle components and seats.</summary>
        public void NotifyStatusEffectTrigger(RemovalTrigger trigger)
        {
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
            TurnEventBus.EmitVehicleDestroyed(this);
        }

        // ==================== OPERATIONAL STATUS ====================

        /// <summary>Null if operational.</summary>
        public string GetNonOperationalReason()
        {
            if (chassis == null) return "No chassis installed";
            if (chassis.IsDestroyed()) return "Chassis destroyed";
            if (powerCore == null) return "No power core installed";
            if (powerCore.IsDestroyed()) return "Power core destroyed - no power";
            return null;
        }

        public bool IsOperational() => GetNonOperationalReason() == null;

        /// <summary>True if any chassis condition is blocking all component actions vehicle-wide.</summary>
        public bool IsChassisStunned
        {
            get
            {
                if (chassis == null) return false;
                foreach (var effect in chassis.GetActiveConditions())
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
            var driveComponent = GetDriveComponent();
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

            // Resource validation
            if (!CanAffordSkill(skill))
            {
                int currentEnergy = powerCore != null ? powerCore.currentEnergy : 0;
                Debug.LogWarning($"[Vehicle] {vehicleName} cannot afford {skill.name} (need {skill.energyCost}, have {currentEnergy})");
                return false;
            }

            // Resource consumption
            VehicleComponent sourceComponent = ctx.SourceActor?.GetEntity() as VehicleComponent;
            if (!ConsumeSkillCost(skill, sourceComponent))
            {
                Debug.LogError($"[Vehicle] {vehicleName} failed to consume resources for {skill.name}");
                return false;
            }

            // Skill configuration validation
            if (!SkillValidator.Validate(ctx, skill))
                return false;

            // Fire OnSkillUsed removal trigger — once per committed skill use
            if (sourceComponent != null)
                sourceComponent.NotifyConditionTrigger(RemovalTrigger.OnSkillUsed);

            // Execute via RollNodeExecutor
            bool result = RollNodeExecutor.Execute(skill.rollNode, ctx);

            if (result && skill is WeaponAttackSkill)
            {
                WeaponComponent weapon = ctx.SourceActor?.GetEntity() as WeaponComponent;
                if (weapon != null && weapon.loadedAmmunition != null && HasChargesFor(weapon.loadedAmmunition))
                {
                    RollNodeExecutor.Execute(weapon.loadedAmmunition.onHitNode, ctx);
                    TrySpendConsumable(weapon.loadedAmmunition, ctx.CausalSource);
                }
            }

            return result;
        }

        private bool CanAffordSkill(Skill skill)
        {
            if (powerCore == null || skill == null) return false;
            return powerCore.CanDrawPower(skill.energyCost, null);
        }

        private bool ConsumeSkillCost(Skill skill, VehicleComponent sourceComponent)
        {
            if (powerCore == null || skill == null) return false;
            return powerCore.DrawPower(skill.energyCost, sourceComponent, $"Skill: {skill.name}");
        }
    }
}