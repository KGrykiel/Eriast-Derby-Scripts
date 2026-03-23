using Assets.Scripts.Combat.Damage;
using Assets.Scripts.Combat.Restoration;
using Assets.Scripts.Combat.Rolls;
using Assets.Scripts.Conditions.EntityConditions;
using Assets.Scripts.Conditions.CharacterConditions;
using Assets.Scripts.Entities.Vehicle;

namespace Assets.Scripts.Combat
{
    /// <summary>
    /// Base class for all combat events - data packets for things that happen during combat
    /// Used for logging.
    /// </summary>
    public abstract class CombatEvent
    {
        public Entity Source { get; set; }
        public Entity Target { get; set; }
        public string CausalSource { get; set; }
    }
    
    public class DamageEvent : CombatEvent
    {
        public DamageResult Result { get; set; }
        public DamageSource SourceType { get; set; }
        
        public DamageEvent(
            DamageResult result,
            Entity source,
            Entity target,
            string causalSource,
            DamageSource sourceType = DamageSource.Ability)
        {
            Result = result;
            Source = source;
            Target = target;
            CausalSource = causalSource;
            SourceType = sourceType;
        }
    }

    public class RestorationEvent : CombatEvent
    {
        public RestorationResult Result { get; set; }
        
        public RestorationEvent(
            RestorationResult result,
            Entity source,
            Entity target)
        {
            Result = result;
            Source = source;
            Target = target;
        }
    }
    
    /// <summary>Attack roll (hit or miss). Separate from damage — attack determines IF damage happens.</summary>
    public class AttackRollEvent : CombatEvent
    {
        public D20RollOutcome Roll { get; set; }
        public RollActor Actor { get; set; }

        public AttackRollEvent(
            D20RollOutcome roll,
            RollActor actor,
            Entity target,
            string causalSource)
        {
            Roll = roll;
            Actor = actor;
            Source = actor?.GetEntity();
            Target = target;
            CausalSource = causalSource;
        }
    }
    
    public class SavingThrowEvent : CombatEvent
    {
        public D20RollOutcome Roll { get; set; }
        public RollActor Defender { get; set; }
        public string CheckName { get; set; }

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
            Target = defender?.GetEntity();
            CausalSource = causalSource;
            CheckName = checkName;
        }
    }
    
    public class SkillCheckEvent : CombatEvent
    {
        public D20RollOutcome Roll { get; set; }
        public RollActor Actor { get; set; }
        public string CheckName { get; set; }

        public SkillCheckEvent(
            D20RollOutcome roll,
            RollActor actor,
            string causalSource,
            string checkName)
        {
            Roll = roll;
            Actor = actor;
            Source = actor?.GetEntity();
            Target = null; // Skill checks don't have a target
            CausalSource = causalSource;
            CheckName = checkName;
        }
    }

    public class OpposedCheckEvent : CombatEvent
    {
        public D20RollOutcome Roll { get; set; }
        public D20RollOutcome DefenderRoll { get; set; }
        public RollActor AttackerActor { get; set; }
        public RollActor DefenderActor { get; set; }
        public string AttackerCheckName { get; set; }
        public string DefenderCheckName { get; set; }

        public OpposedCheckEvent(
            D20RollOutcome roll,
            D20RollOutcome defenderRoll,
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
            Source = attackerActor?.GetEntity();
            Target = defenderActor?.GetEntity();
            CausalSource = causalSource;
            AttackerCheckName = attackerCheckName;
            DefenderCheckName = defenderCheckName;
        }
    }


    public class EntityConditionEvent : CombatEvent
    {
        public AppliedEntityCondition Applied { get; set; }
        public bool WasReplacement { get; set; }

        /// <summary>True if requirements not met or better effect exists.</summary>
        public bool WasBlocked { get; set; }

        public EntityConditionEvent(
            AppliedEntityCondition applied,
            Entity source,
            Entity target,
            string causalSource,
            bool wasReplacement = false)
        {
            Applied = applied;
            Source = source;
            Target = target;
            CausalSource = causalSource;
            WasReplacement = wasReplacement;
            WasBlocked = applied == null;
        }
    }

    public class EntityConditionExpiredEvent : CombatEvent
    {
        public AppliedEntityCondition Expired { get; set; }

        public EntityConditionExpiredEvent(AppliedEntityCondition expired, Entity target)
        {
            Expired = expired;
            Target = target;
            Source = null;
        }
    }

    public class EntityConditionRefreshedEvent : CombatEvent
    {
        public AppliedEntityCondition Refreshed { get; set; }

        public EntityConditionRefreshedEvent(AppliedEntityCondition refreshed, Entity target)
        {
            Refreshed = refreshed;
            Target = target;
            Source = null;
        }
    }

    public class EntityConditionIgnoredEvent : CombatEvent
    {
        public AppliedEntityCondition Existing { get; set; }

        public EntityConditionIgnoredEvent(AppliedEntityCondition existing, Entity target)
        {
            Existing = existing;
            Target = target;
            Source = null;
        }
    }

    public class EntityConditionReplacedEvent : CombatEvent
    {
        public AppliedEntityCondition NewEffect { get; set; }
        public int OldDuration { get; set; }

        public EntityConditionReplacedEvent(AppliedEntityCondition newEffect, Entity target, int oldDuration)
        {
            NewEffect = newEffect;
            Target = target;
            OldDuration = oldDuration;
            Source = null;
        }
    }

    public class EntityConditionKeptStrongerEvent : CombatEvent
    {
        public AppliedEntityCondition Kept { get; set; }

        public EntityConditionKeptStrongerEvent(AppliedEntityCondition kept, Entity target)
        {
            Kept = kept;
            Target = target;
            Source = null;
        }
    }

    public class EntityConditionStackLimitEvent : CombatEvent
    {
        public EntityCondition Template { get; set; }
        public int MaxStacks { get; set; }

        public EntityConditionStackLimitEvent(EntityCondition template, Entity target, int maxStacks)
        {
            Template = template;
            Target = target;
            MaxStacks = maxStacks;
            Source = null;
        }
    }

    // ==================== CHARACTER CONDITION EVENTS ====================

    public class CharacterConditionEvent : CombatEvent
    {
        public AppliedCharacterCondition Applied { get; set; }
        public VehicleSeat TargetSeat { get; set; }

        public CharacterConditionEvent(
            AppliedCharacterCondition applied,
            Entity source,
            VehicleSeat targetSeat,
            string causalSource)
        {
            Applied = applied;
            Source = source;
            Target = null;
            TargetSeat = targetSeat;
            CausalSource = causalSource;
        }
    }

    public class CharacterConditionExpiredEvent : CombatEvent
    {
        public AppliedCharacterCondition Expired { get; set; }
        public VehicleSeat TargetSeat { get; set; }

        public CharacterConditionExpiredEvent(AppliedCharacterCondition expired, VehicleSeat targetSeat)
        {
            Expired = expired;
            TargetSeat = targetSeat;
            Source = null;
            Target = null;
        }
    }

    public class CharacterConditionRefreshedEvent : CombatEvent
    {
        public AppliedCharacterCondition Refreshed { get; set; }
        public VehicleSeat TargetSeat { get; set; }

        public CharacterConditionRefreshedEvent(AppliedCharacterCondition refreshed, VehicleSeat targetSeat)
        {
            Refreshed = refreshed;
            TargetSeat = targetSeat;
            Source = null;
            Target = null;
        }
    }

    public class CharacterConditionIgnoredEvent : CombatEvent
    {
        public AppliedCharacterCondition Existing { get; set; }
        public VehicleSeat TargetSeat { get; set; }

        public CharacterConditionIgnoredEvent(AppliedCharacterCondition existing, VehicleSeat targetSeat)
        {
            Existing = existing;
            TargetSeat = targetSeat;
            Source = null;
            Target = null;
        }
    }

    public class CharacterConditionReplacedEvent : CombatEvent
    {
        public AppliedCharacterCondition NewCondition { get; set; }
        public int OldDuration { get; set; }
        public VehicleSeat TargetSeat { get; set; }

        public CharacterConditionReplacedEvent(AppliedCharacterCondition newCondition, VehicleSeat targetSeat, int oldDuration)
        {
            NewCondition = newCondition;
            TargetSeat = targetSeat;
            OldDuration = oldDuration;
            Source = null;
            Target = null;
        }
    }

    public class CharacterConditionKeptStrongerEvent : CombatEvent
    {
        public AppliedCharacterCondition Kept { get; set; }
        public VehicleSeat TargetSeat { get; set; }

        public CharacterConditionKeptStrongerEvent(AppliedCharacterCondition kept, VehicleSeat targetSeat)
        {
            Kept = kept;
            TargetSeat = targetSeat;
            Source = null;
            Target = null;
        }
    }

    public class CharacterConditionStackLimitEvent : CombatEvent
    {
        public CharacterCondition Template { get; set; }
        public int MaxStacks { get; set; }
        public VehicleSeat TargetSeat { get; set; }

        public CharacterConditionStackLimitEvent(CharacterCondition template, VehicleSeat targetSeat, int maxStacks)
        {
            Template = template;
            TargetSeat = targetSeat;
            MaxStacks = maxStacks;
            Source = null;
            Target = null;
        }
    }
}

