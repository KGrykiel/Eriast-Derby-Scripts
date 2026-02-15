using UnityEngine;
using Assets.Scripts.Combat.SkillChecks;
using Assets.Scripts.Combat.Saves;
using Assets.Scripts.Combat.Damage;
using Assets.Scripts.StatusEffects;
using Assets.Scripts.Combat.Attacks;

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
        public Object CausalSource { get; set; }
    }
    
    public class DamageEvent : CombatEvent
    {
        public DamageResult Result { get; set; }
        public DamageSource SourceType { get; set; }
        
        public DamageEvent(
            DamageResult result,
            Entity source,
            Entity target,
            Object causalSource,
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
            Object causalSource,
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
            CausalSource = expired?.template;
        }
    }
    
    public class RestorationEvent : CombatEvent
    {
        public RestorationBreakdown Breakdown { get; set; }
        
        public RestorationEvent(
            RestorationBreakdown breakdown,
            Entity source,
            Entity target,
            Object causalSource)
        {
            Breakdown = breakdown;
            Source = source;
            Target = target;
            CausalSource = causalSource;
        }
    }
    
    /// <summary>Attack roll (hit or miss). Separate from damage — attack determines IF damage happens.</summary>
    public class AttackRollEvent : CombatEvent
    {
        public AttackResult Result { get; set; }
        public bool IsHit { get; set; }
        public string TargetComponentName { get; set; }
        public bool IsChassisFallback { get; set; }

        /// <summary>Null for component-only or standalone entity attacks.</summary>
        public Character Character { get; set; }
        
        public AttackRollEvent(
            AttackResult result,
            Entity source,
            Entity target,
            UnityEngine.Object causalSource,
            bool isHit,
            string targetComponentName = null,
            bool isChassisFallback = false,
            Character character = null)
        {
            Result = result;
            Source = source;
            Target = target;
            CausalSource = causalSource;
            IsHit = isHit;
            TargetComponentName = targetComponentName;
            IsChassisFallback = isChassisFallback;
            Character = character;
        }
    }
    
    public class SavingThrowEvent : CombatEvent
    {
        public SaveResult Result { get; set; }
        public bool Succeeded { get; set; }
        public string TargetComponentName { get; set; }

        /// <summary>Null for vehicle-only saves.</summary>
        public Character Character { get; set; }
        
        public SavingThrowEvent(
            SaveResult result,
            Entity source,
            Entity target,
            UnityEngine.Object causalSource,
            bool succeeded,
            string targetComponentName = null,
            Character character = null)
        {
            Result = result;
            Source = source;
            Target = target;
            CausalSource = causalSource;
            Succeeded = succeeded;
            TargetComponentName = targetComponentName;
            Character = character;
        }
    }
    
    public class SkillCheckEvent : CombatEvent
    {
        public SkillCheckResult Result { get; set; }
        public bool Succeeded { get; set; }

        /// <summary>Null for vehicle-only checks.</summary>
        public Character Character { get; set; }
        
        public SkillCheckEvent(
            SkillCheckResult result,
            Entity source,
            UnityEngine.Object causalSource,
            bool succeeded,
            Character character = null)
        {
            Result = result;
            Source = source;
            Target = null; // Skill checks don't have a target
            CausalSource = causalSource;
            Succeeded = succeeded;
            Character = character;
        }
    }
}

