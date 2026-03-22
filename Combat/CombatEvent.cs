using Assets.Scripts.Combat.Damage;
using Assets.Scripts.Combat.Restoration;
using Assets.Scripts.StatusEffects;
using Assets.Scripts.Combat.Rolls;

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
    
    public class StatusEffectEvent : CombatEvent
    {
        public AppliedStatusEffect Applied { get; set; }
        public bool WasReplacement { get; set; }

        /// <summary>True if requirements not met or better effect exists.</summary>
        public bool WasBlocked { get; set; }
        
        public StatusEffectEvent(
            AppliedStatusEffect applied,
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
    
    public class StatusEffectExpiredEvent : CombatEvent
    {
        public AppliedStatusEffect Expired { get; set; }

        public StatusEffectExpiredEvent(AppliedStatusEffect expired, Entity target)
        {
            Expired = expired;
            Target = target;
            Source = null;
        }
    }

    public class StatusEffectRefreshedEvent : CombatEvent
    {
        public AppliedStatusEffect Refreshed { get; set; }

        public StatusEffectRefreshedEvent(AppliedStatusEffect refreshed, Entity target)
        {
            Refreshed = refreshed;
            Target = target;
            Source = null;
        }
    }

    public class StatusEffectIgnoredEvent : CombatEvent
    {
        public AppliedStatusEffect Existing { get; set; }

        public StatusEffectIgnoredEvent(AppliedStatusEffect existing, Entity target)
        {
            Existing = existing;
            Target = target;
            Source = null;
        }
    }

    public class StatusEffectReplacedEvent : CombatEvent
    {
        public AppliedStatusEffect NewEffect { get; set; }
        public int OldDuration { get; set; }

        public StatusEffectReplacedEvent(AppliedStatusEffect newEffect, Entity target, int oldDuration)
        {
            NewEffect = newEffect;
            Target = target;
            OldDuration = oldDuration;
            Source = null;
        }
    }

    public class StatusEffectKeptStrongerEvent : CombatEvent
    {
        public AppliedStatusEffect Kept { get; set; }

        public StatusEffectKeptStrongerEvent(AppliedStatusEffect kept, Entity target)
        {
            Kept = kept;
            Target = target;
            Source = null;
        }
    }

    public class StatusEffectStackLimitEvent : CombatEvent
    {
        public StatusEffect Template { get; set; }
        public int MaxStacks { get; set; }

        public StatusEffectStackLimitEvent(StatusEffect template, Entity target, int maxStacks)
        {
            Template = template;
            Target = target;
            MaxStacks = maxStacks;
            Source = null;
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
        public bool IsAutoFail { get; set; }

        public SavingThrowEvent(
            D20RollOutcome roll,
            Entity source,
            RollActor defender,
            string causalSource,
            string checkName,
            bool isAutoFail = false)
        {
            Roll = roll;
            Defender = defender;
            Source = source;
            Target = defender?.GetEntity();
            CausalSource = causalSource;
            CheckName = checkName;
            IsAutoFail = isAutoFail;
        }
    }
    
    public class SkillCheckEvent : CombatEvent
    {
        public D20RollOutcome Roll { get; set; }
        public RollActor Actor { get; set; }
        public string CheckName { get; set; }
        public bool IsAutoFail { get; set; }

        public SkillCheckEvent(
            D20RollOutcome roll,
            RollActor actor,
            string causalSource,
            string checkName,
            bool isAutoFail = false)
        {
            Roll = roll;
            Actor = actor;
            Source = actor?.GetEntity();
            Target = null; // Skill checks don't have a target
            CausalSource = causalSource;
            CheckName = checkName;
            IsAutoFail = isAutoFail;
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
}

