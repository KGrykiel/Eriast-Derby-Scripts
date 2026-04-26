using Assets.Scripts.Combat.Damage;
using Assets.Scripts.Combat.Restoration;
using Assets.Scripts.Combat.Rolls;
using Assets.Scripts.Conditions;
using Assets.Scripts.Conditions.EntityConditions;
using Assets.Scripts.Conditions.CharacterConditions;
using Assets.Scripts.Conditions.VehicleConditions;
using Assets.Scripts.Items;
using Assets.Scripts.Entities.Vehicles;
using Assets.Scripts.Entities;
using Assets.Scripts.Stages.Lanes;

namespace Assets.Scripts.Combat
{
    /// <summary>
    /// Base class for all combat events - data packets for things that happen during combat
    /// Used for logging.
    /// </summary>
    public abstract class CombatEvent
    {
        public string CausalSource { get; protected set; }
    }
    
    public class DamageEvent : CombatEvent
    {
        public DamageResult Result { get; private set; }
        public RollActor Actor { get; private set; }
        public Entity Target { get; private set; }

        public DamageEvent(
            DamageResult result,
            RollActor actor,
            Entity target,
            string causalSource)
        {
            Result = result;
            Actor = actor;
            Target = target;
            CausalSource = causalSource;
        }
    }

    public class RestorationEvent : CombatEvent
    {
        public RestorationResult Result { get; private set; }
        public RollActor Actor { get; private set; }
        public Entity Target { get; private set; }

        public RestorationEvent(
            RestorationResult result,
            RollActor actor,
            Entity target,
            string causalSource)
        {
            Result = result;
            Actor = actor;
            Target = target;
            CausalSource = causalSource;
        }
    }
    
    /// <summary>Deterministic state threshold check — no d20 roll, pass/fail based on a live vehicle value vs a minimum.</summary>
    public class StateThresholdEvent : CombatEvent
    {
        public Vehicle Target { get; private set; }
        public Vehicle.RuntimeState State { get; private set; }
        public int CurrentValue { get; private set; }
        public int MinimumValue { get; private set; }
        public bool Success { get; private set; }

        public StateThresholdEvent(
            Vehicle target,
            Vehicle.RuntimeState state,
            int currentValue,
            int minimumValue,
            bool success,
            string causalSource)
        {
            Target = target;
            State = state;
            CurrentValue = currentValue;
            MinimumValue = minimumValue;
            Success = success;
            CausalSource = causalSource;
        }
    }

    /// <summary>Attack roll (hit or miss). Separate from damage — attack determines IF damage happens.</summary>
    public class AttackRollEvent : CombatEvent
    {
        public D20RollOutcome Roll { get; private set; }
        public RollActor Actor { get; private set; }
        public Entity Target { get; private set; }

        public AttackRollEvent(
            D20RollOutcome roll,
            RollActor actor,
            Entity target,
            string causalSource)
        {
            Roll = roll;
            Actor = actor;
            Target = target;
            CausalSource = causalSource;
        }
    }
    
    public class SavingThrowEvent : CombatEvent
    {
        public D20RollOutcome Roll { get; private set; }
        public RollActor Defender { get; private set; }
        public string CheckName { get; private set; }
        public Entity Source { get; private set; }

        public SavingThrowEvent(
            D20RollOutcome roll,
            Entity source,
            RollActor defender,
            string causalSource,
            string checkName)
        {
            Roll = roll;
            Defender = defender;
            Source = source;
            CausalSource = causalSource;
            CheckName = checkName;
        }
    }
    
    public class SkillCheckEvent : CombatEvent
    {
        public D20RollOutcome Roll { get; private set; }
        public RollActor Actor { get; private set; }
        public string CheckName { get; private set; }

        public SkillCheckEvent(
            D20RollOutcome roll,
            RollActor actor,
            string causalSource,
            string checkName)
        {
            Roll = roll;
            Actor = actor;
            CausalSource = causalSource;
            CheckName = checkName;
        }
    }

    public class OpposedCheckEvent : CombatEvent
    {
        public D20RollOutcome Roll { get; private set; }
        public D20RollOutcome? DefenderRoll { get; private set; }
        public RollActor AttackerActor { get; private set; }
        public RollActor DefenderActor { get; private set; }
        public string AttackerCheckName { get; private set; }
        public string DefenderCheckName { get; private set; }

        public OpposedCheckEvent(
            D20RollOutcome roll,
            D20RollOutcome? defenderRoll,
            RollActor attackerActor,
            RollActor defenderActor,
            string causalSource,
            string attackerCheckName,
            string defenderCheckName)
        {
            Roll = roll;
            DefenderRoll = defenderRoll;
            AttackerActor = attackerActor;
            DefenderActor = defenderActor;
            CausalSource = causalSource;
            AttackerCheckName = attackerCheckName;
            DefenderCheckName = defenderCheckName;
        }
    }


    public class EntityConditionEvent : CombatEvent
    {
        public AppliedEntityCondition Applied { get; private set; }
        public Entity Source { get; private set; }
        public Entity Target { get; private set; }

        public EntityConditionEvent(
            AppliedEntityCondition applied,
            Entity source,
            Entity target,
            string causalSource)
        {
            Applied = applied;
            Source = source;
            Target = target;
            CausalSource = causalSource;
        }
    }

    // ==================== ENTITY CONDITION EVENTS ====================

    public class EntityConditionExpiredEvent : CombatEvent
    {
        public AppliedEntityCondition Expired { get; private set; }
        public Entity Target { get; private set; }

        public EntityConditionExpiredEvent(AppliedEntityCondition expired, Entity target)
        {
            Expired = expired;
            Target = target;
        }
    }

    public class EntityConditionRefreshedEvent : CombatEvent
    {
        public AppliedEntityCondition Refreshed { get; private set; }
        public Entity Target { get; private set; }

        public EntityConditionRefreshedEvent(AppliedEntityCondition refreshed, Entity target)
        {
            Refreshed = refreshed;
            Target = target;
        }
    }

    public class EntityConditionIgnoredEvent : CombatEvent
    {
        public AppliedEntityCondition Existing { get; private set; }
        public Entity Target { get; private set; }

        public EntityConditionIgnoredEvent(AppliedEntityCondition existing, Entity target)
        {
            Existing = existing;
            Target = target;
        }
    }

    public class EntityConditionReplacedEvent : CombatEvent
    {
        public AppliedEntityCondition NewEffect { get; private set; }
        public int OldDuration { get; private set; }
        public Entity Target { get; private set; }

        public EntityConditionReplacedEvent(AppliedEntityCondition newEffect, Entity target, int oldDuration)
        {
            NewEffect = newEffect;
            Target = target;
            OldDuration = oldDuration;
        }
    }

    public class EntityConditionKeptStrongerEvent : CombatEvent
    {
        public AppliedEntityCondition Kept { get; private set; }
        public Entity Target { get; private set; }

        public EntityConditionKeptStrongerEvent(AppliedEntityCondition kept, Entity target)
        {
            Kept = kept;
            Target = target;
        }
    }

    public class EntityConditionStackLimitEvent : CombatEvent
    {
        public EntityCondition Template { get; private set; }
        public int MaxStacks { get; private set; }
        public Entity Target { get; private set; }

        public EntityConditionStackLimitEvent(EntityCondition template, Entity target, int maxStacks)
        {
            Template = template;
            Target = target;
            MaxStacks = maxStacks;
        }
    }

    public class EntityConditionRemovedByTriggerEvent : CombatEvent
    {
        public AppliedEntityCondition Removed { get; private set; }
        public RemovalTrigger Trigger { get; private set; }
        public Entity Target { get; private set; }

        public EntityConditionRemovedByTriggerEvent(AppliedEntityCondition removed, Entity target, RemovalTrigger trigger)
        {
            Removed = removed;
            Target = target;
            Trigger = trigger;
        }
    }

    // ==================== CHARACTER CONDITION EVENTS ====================

    public class CharacterConditionEvent : CombatEvent
    {
        public AppliedCharacterCondition Applied { get; private set; }
        public VehicleSeat TargetSeat { get; private set; }
        public Entity Source { get; private set; }

        public CharacterConditionEvent(
            AppliedCharacterCondition applied,
            Entity source,
            VehicleSeat targetSeat,
            string causalSource)
        {
            Applied = applied;
            Source = source;
            TargetSeat = targetSeat;
            CausalSource = causalSource;
        }
    }

    public class CharacterConditionExpiredEvent : CombatEvent
    {
        public AppliedCharacterCondition Expired { get; private set; }
        public VehicleSeat TargetSeat { get; private set; }

        public CharacterConditionExpiredEvent(AppliedCharacterCondition expired, VehicleSeat targetSeat)
        {
            Expired = expired;
            TargetSeat = targetSeat;
        }
    }

    public class CharacterConditionRefreshedEvent : CombatEvent
    {
        public AppliedCharacterCondition Refreshed { get; private set; }
        public VehicleSeat TargetSeat { get; private set; }

        public CharacterConditionRefreshedEvent(AppliedCharacterCondition refreshed, VehicleSeat targetSeat)
        {
            Refreshed = refreshed;
            TargetSeat = targetSeat;
        }
    }

    public class CharacterConditionIgnoredEvent : CombatEvent
    {
        public AppliedCharacterCondition Existing { get; private set; }
        public VehicleSeat TargetSeat { get; private set; }

        public CharacterConditionIgnoredEvent(AppliedCharacterCondition existing, VehicleSeat targetSeat)
        {
            Existing = existing;
            TargetSeat = targetSeat;
        }
    }

    public class CharacterConditionReplacedEvent : CombatEvent
    {
        public AppliedCharacterCondition NewCondition { get; private set; }
        public int OldDuration { get; private set; }
        public VehicleSeat TargetSeat { get; private set; }

        public CharacterConditionReplacedEvent(AppliedCharacterCondition newCondition, VehicleSeat targetSeat, int oldDuration)
        {
            NewCondition = newCondition;
            TargetSeat = targetSeat;
            OldDuration = oldDuration;
        }
    }

    public class CharacterConditionKeptStrongerEvent : CombatEvent
    {
        public AppliedCharacterCondition Kept { get; private set; }
        public VehicleSeat TargetSeat { get; private set; }

        public CharacterConditionKeptStrongerEvent(AppliedCharacterCondition kept, VehicleSeat targetSeat)
        {
            Kept = kept;
            TargetSeat = targetSeat;
        }
    }

    public class CharacterConditionStackLimitEvent : CombatEvent
    {
        public CharacterCondition Template { get; private set; }
        public int MaxStacks { get; private set; }
        public VehicleSeat TargetSeat { get; private set; }

        public CharacterConditionStackLimitEvent(CharacterCondition template, VehicleSeat targetSeat, int maxStacks)
        {
            Template = template;
            TargetSeat = targetSeat;
            MaxStacks = maxStacks;
        }
    }

    public class CharacterConditionRemovedByTriggerEvent : CombatEvent
    {
        public AppliedCharacterCondition Removed { get; private set; }
        public RemovalTrigger Trigger { get; private set; }
        public VehicleSeat TargetSeat { get; private set; }

        public CharacterConditionRemovedByTriggerEvent(AppliedCharacterCondition removed, VehicleSeat targetSeat, RemovalTrigger trigger)
        {
            Removed = removed;
            TargetSeat = targetSeat;
            Trigger = trigger;
        }
    }

    // ==================== CONSUMABLE EVENTS ====================

    public class ConsumableSpentEvent : CombatEvent
    {
        public ItemBase Template { get; private set; }
        public Vehicle Vehicle { get; private set; }
        public int ChargesRemaining { get; private set; }

        public ConsumableSpentEvent(ItemBase template, Vehicle vehicle, string causalSource, int chargesRemaining)
        {
            Template = template;
            Vehicle = vehicle;
            CausalSource = causalSource;
            ChargesRemaining = chargesRemaining;
        }
    }

    public class ConsumableRestoredEvent : CombatEvent
    {
        public ItemBase Template { get; private set; }
        public Vehicle Vehicle { get; private set; }
        public int Amount { get; private set; }
        public int ChargesAfter { get; private set; }

        public ConsumableRestoredEvent(ItemBase template, Vehicle vehicle, string causalSource, int amount, int chargesAfter)
        {
            Template = template;
            Vehicle = vehicle;
            CausalSource = causalSource;
            Amount = amount;
            ChargesAfter = chargesAfter;
        }
    }

    public class ConsumableUnavailableEvent : CombatEvent
    {
        public ItemBase Template { get; private set; }
        public Vehicle Vehicle { get; private set; }

        public ConsumableUnavailableEvent(ItemBase template, Vehicle vehicle, string causalSource)
        {
            Template = template;
            Vehicle = vehicle;
            CausalSource = causalSource;
        }
    }

    // ==================== VEHICLE CONDITION EVENTS ====================

    public class VehicleConditionEvent : CombatEvent
    {
        public AppliedVehicleCondition Applied { get; private set; }
        public Entity Source { get; private set; }
        public Vehicle Target { get; private set; }

        public VehicleConditionEvent(
            AppliedVehicleCondition applied,
            Entity source,
            Vehicle target,
            string causalSource)
        {
            Applied = applied;
            Source = source;
            Target = target;
            CausalSource = causalSource;
        }
    }

    public class VehicleConditionExpiredEvent : CombatEvent
    {
        public AppliedVehicleCondition Expired { get; private set; }
        public Vehicle Target { get; private set; }

        public VehicleConditionExpiredEvent(AppliedVehicleCondition expired, Vehicle target)
        {
            Expired = expired;
            Target = target;
        }
    }

    public class VehicleConditionRefreshedEvent : CombatEvent
    {
        public AppliedVehicleCondition Refreshed { get; private set; }
        public Vehicle Target { get; private set; }

        public VehicleConditionRefreshedEvent(AppliedVehicleCondition refreshed, Vehicle target)
        {
            Refreshed = refreshed;
            Target = target;
        }
    }

    public class VehicleConditionIgnoredEvent : CombatEvent
    {
        public AppliedVehicleCondition Existing { get; private set; }
        public Vehicle Target { get; private set; }

        public VehicleConditionIgnoredEvent(AppliedVehicleCondition existing, Vehicle target)
        {
            Existing = existing;
            Target = target;
        }
    }

    public class VehicleConditionReplacedEvent : CombatEvent
    {
        public AppliedVehicleCondition NewCondition { get; private set; }
        public int OldDuration { get; private set; }
        public Vehicle Target { get; private set; }

        public VehicleConditionReplacedEvent(AppliedVehicleCondition newCondition, Vehicle target, int oldDuration)
        {
            NewCondition = newCondition;
            Target = target;
            OldDuration = oldDuration;
        }
    }

    public class VehicleConditionKeptStrongerEvent : CombatEvent
    {
        public AppliedVehicleCondition Kept { get; private set; }
        public Vehicle Target { get; private set; }

        public VehicleConditionKeptStrongerEvent(AppliedVehicleCondition kept, Vehicle target)
        {
            Kept = kept;
            Target = target;
        }
    }

    public class VehicleConditionStackLimitEvent : CombatEvent
    {
        public VehicleCondition Template { get; private set; }
        public int MaxStacks { get; private set; }
        public Vehicle Target { get; private set; }

        public VehicleConditionStackLimitEvent(VehicleCondition template, Vehicle target, int maxStacks)
        {
            Template = template;
            Target = target;
            MaxStacks = maxStacks;
        }
    }

    public class VehicleConditionRemovedByTriggerEvent : CombatEvent
    {
        public AppliedVehicleCondition Removed { get; private set; }
        public RemovalTrigger Trigger { get; private set; }
        public Vehicle Target { get; private set; }

        public VehicleConditionRemovedByTriggerEvent(AppliedVehicleCondition removed, Vehicle target, RemovalTrigger trigger)
        {
            Removed = removed;
            Target = target;
            Trigger = trigger;
        }
    }

    // ==================== MOVEMENT EVENTS ====================

    public class LaneChangeEvent : CombatEvent
    {
        public Vehicle Target { get; private set; }
        public StageLane FromLane { get; private set; }
        public StageLane ToLane { get; private set; }

        public LaneChangeEvent(Vehicle target, StageLane fromLane, StageLane toLane, string causalSource)
        {
            Target = target;
            FromLane = fromLane;
            ToLane = toLane;
            CausalSource = causalSource;
        }
    }

    public class ProgressModifierEvent : CombatEvent
    {
        public Vehicle Target { get; private set; }
        public int OldProgress { get; private set; }
        public int NewProgress { get; private set; }
        public int Delta { get; private set; }

        public ProgressModifierEvent(Vehicle target, int oldProgress, int newProgress, string causalSource)
        {
            Target = target;
            OldProgress = oldProgress;
            NewProgress = newProgress;
            Delta = newProgress - oldProgress;
            CausalSource = causalSource;
        }
    }

    public class SpeedChangeEvent : CombatEvent
    {
        public Vehicle Target { get; private set; }
        public int OldSpeedPercent { get; private set; }
        public int NewSpeedPercent { get; private set; }

        public SpeedChangeEvent(Vehicle target, int oldSpeedPercent, int newSpeedPercent, string causalSource)
        {
            Target = target;
            OldSpeedPercent = oldSpeedPercent;
            NewSpeedPercent = newSpeedPercent;
            CausalSource = causalSource;
        }
    }
}


