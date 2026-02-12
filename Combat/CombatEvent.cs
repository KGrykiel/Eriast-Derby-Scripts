using UnityEngine;
using Assets.Scripts.Combat.SkillChecks;
using Assets.Scripts.Combat.Saves;
using Assets.Scripts.Combat.Damage;
using Assets.Scripts.StatusEffects;
using Assets.Scripts.Combat.Attacks;

namespace Assets.Scripts.Combat
{
    /// <summary>
    /// Base class for all combat events.
    /// Events are data packets that describe what happened, not how to display it.
    /// CombatLogManager handles formatting and logging.
    /// </summary>
    public abstract class CombatEvent
    {
        /// <summary>Entity that caused the event (attacker, caster, etc.)</summary>
        public Entity Source { get; set; }
        
        /// <summary>Entity that received the event (target of damage/buff/etc.)</summary>
        public Entity Target { get; set; }
        
        /// <summary>What triggered this event (Skill, StatusEffect, Stage, etc.)</summary>
        public UnityEngine.Object CausalSource { get; set; }
        
        /// <summary>When this event occurred</summary>
        public float Timestamp { get; set; }
        
        protected CombatEvent()
        {
            Timestamp = Time.time;
        }
    }
    
    /// <summary>
    /// Event emitted when damage is dealt.
    /// Contains full breakdown for tooltip display.
    /// </summary>
    public class DamageEvent : CombatEvent
    {
        /// <summary>Full damage result with sources, resistances, etc.</summary>
        public DamageResult Result { get; set; }
        
        /// <summary>Category of damage source (Weapon, Ability, Effect, Environmental)</summary>
        public DamageSource SourceType { get; set; }
        
        public DamageEvent(
            DamageResult result,
            Entity source,
            Entity target,
            UnityEngine.Object causalSource,
            DamageSource sourceType = DamageSource.Ability)
        {
            Result = result;
            Source = source;
            Target = target;
            CausalSource = causalSource;
            SourceType = sourceType;
        }
    }
    
    /// <summary>
    /// Event emitted when a status effect is applied.
    /// </summary>
    public class StatusEffectEvent : CombatEvent
    {
        /// <summary>The applied status effect instance</summary>
        public AppliedStatusEffect Applied { get; set; }
        
        /// <summary>Whether this replaced an existing effect of same type</summary>
        public bool WasReplacement { get; set; }
        
        /// <summary>Whether application failed (requirements not met, better effect exists)</summary>
        public bool WasBlocked { get; set; }
        
        /// <summary>Reason for blocking if WasBlocked is true</summary>
        public string BlockReason { get; set; }
        
        public StatusEffectEvent(
            AppliedStatusEffect applied,
            Entity source,
            Entity target,
            UnityEngine.Object causalSource,
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
    
    /// <summary>
    /// Event emitted when a status effect expires.
    /// </summary>
    public class StatusEffectExpiredEvent : CombatEvent
    {
        /// <summary>The status effect that expired</summary>
        public AppliedStatusEffect Expired { get; set; }
        
        public StatusEffectExpiredEvent(AppliedStatusEffect expired, Entity target)
        {
            Expired = expired;
            Target = target;
            Source = null;
            CausalSource = expired?.template;
        }
    }
    
    /// <summary>
    /// Event emitted when health or energy is restored/drained.
    /// </summary>
    public class RestorationEvent : CombatEvent
    {
        /// <summary>Full restoration breakdown</summary>
        public RestorationBreakdown Breakdown { get; set; }
        
        public RestorationEvent(
            RestorationBreakdown breakdown,
            Entity source,
            Entity target,
            UnityEngine.Object causalSource)
        {
            Breakdown = breakdown;
            Source = source;
            Target = target;
            CausalSource = causalSource;
        }
    }
    
    /// <summary>
    /// Event emitted when an attack roll is made (hit or miss).
    /// Separate from damage - attack determines IF damage happens.
    /// </summary>
    public class AttackRollEvent : CombatEvent
    {
        /// <summary>Full attack result with modifiers</summary>
        public AttackResult Result { get; set; }
        
        /// <summary>Whether the attack hit</summary>
        public bool IsHit { get; set; }
        
        /// <summary>Name of targeted component (if component targeting)</summary>
        public string TargetComponentName { get; set; }
        
        /// <summary>Whether this was a fallback to chassis after missing component</summary>
        public bool IsChassisFallback { get; set; }
        
        /// <summary>
        /// Character who made the attack (null for component-only or standalone entity attacks).
        /// Used for logging "Ada attacks" vs "Weapon attacks".
        /// </summary>
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
    
    /// <summary>
    /// Event emitted when a saving throw is made (target resisting an effect).
    /// Separate from attack rolls - saves are defensive reactions.
    /// </summary>
    public class SavingThrowEvent : CombatEvent
    {
        /// <summary>Full save result with modifiers</summary>
        public SaveResult Result { get; set; }
        
        /// <summary>Whether the target successfully saved (resisted the effect)</summary>
        public bool Succeeded { get; set; }
        
        /// <summary>Name of targeted component (if component targeting)</summary>
        public string TargetComponentName { get; set; }
        
        /// <summary>
        /// Character who made the save (null for vehicle-only saves).
        /// Used for logging "Technician saves vs EMP" vs "Vehicle saves vs Fireball".
        /// Enables character-specific save breakdowns in tooltips.
        /// </summary>
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
    
    /// <summary>
    /// Event emitted when a skill check is made (character attempting a task).
    /// Used for piloting, perception, mechanics, etc.
    /// </summary>
    public class SkillCheckEvent : CombatEvent
    {
        /// <summary>Full skill check result with modifiers</summary>
        public SkillCheckResult Result { get; set; }
        
        /// <summary>Whether the check succeeded</summary>
        public bool Succeeded { get; set; }
        
        /// <summary>
        /// Character who made the check (null for vehicle-only checks).
        /// Used for logging "Pilot makes Piloting check" vs "Vehicle makes Stability check".
        /// Enables character-specific skill breakdowns in tooltips.
        /// </summary>
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

