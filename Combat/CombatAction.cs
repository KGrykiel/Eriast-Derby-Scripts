using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets.Scripts.Combat
{
    /// <summary>
    /// Represents a single combat action (skill use, attack, etc.).
    /// Contains all events that occurred during this action for aggregated logging.
    /// 
    /// Example: A skill that deals 1d8 Physical + 2d6 Fire damage creates:
    /// - 1 CombatAction (the skill)
    /// - 2 DamageEvents (one per damage type)
    /// - Logged as: "Deals 5 Physical + 8 Fire (13 total) damage"
    /// </summary>
    public class CombatAction
    {
        /// <summary>Entity performing the action</summary>
        public Entity Actor { get; set; }
        
        /// <summary>What triggered this action (Skill, EventCard, Stage, etc.)</summary>
        public UnityEngine.Object Source { get; set; }
        
        /// <summary>Primary target of the action (for vehicle-level targeting)</summary>
        public Vehicle PrimaryTarget { get; set; }
        
        /// <summary>All events that occurred during this action</summary>
        public List<CombatEvent> Events { get; } = new List<CombatEvent>();
        
        /// <summary>When this action started</summary>
        public float StartTime { get; set; }
        
        /// <summary>When this action ended</summary>
        public float EndTime { get; set; }
        
        public CombatAction(Entity actor, UnityEngine.Object source, Vehicle primaryTarget = null)
        {
            Actor = actor;
            Source = source;
            PrimaryTarget = primaryTarget;
            StartTime = Time.time;
        }
        
        /// <summary>Add an event to this action</summary>
        public void AddEvent(CombatEvent evt)
        {
            Events.Add(evt);
        }
        
        /// <summary>End this action (sets end time)</summary>
        public void Complete()
        {
            EndTime = Time.time;
        }
        
        // ==================== QUERY HELPERS ====================
        
        /// <summary>Get all damage events</summary>
        public IEnumerable<DamageEvent> GetDamageEvents() 
            => Events.OfType<DamageEvent>();
        
        /// <summary>Get all status effect events</summary>
        public IEnumerable<StatusEffectEvent> GetStatusEffectEvents() 
            => Events.OfType<StatusEffectEvent>();
        
        /// <summary>Get all restoration events</summary>
        public IEnumerable<RestorationEvent> GetRestorationEvents() 
            => Events.OfType<RestorationEvent>();
        
        /// <summary>Get all attack roll events</summary>
        public IEnumerable<AttackRollEvent> GetAttackRollEvents() 
            => Events.OfType<AttackRollEvent>();
        
        /// <summary>Get all saving throw events</summary>
        public IEnumerable<SavingThrowEvent> GetSavingThrowEvents() 
            => Events.OfType<SavingThrowEvent>();
        
        /// <summary>Get all skill check events</summary>
        public IEnumerable<SkillCheckEvent> GetSkillCheckEvents() 
            => Events.OfType<SkillCheckEvent>();
        
        /// <summary>Get damage events grouped by target</summary>
        public Dictionary<Entity, List<DamageEvent>> GetDamageByTarget()
        {
            return GetDamageEvents()
                .GroupBy(e => e.Target)
                .ToDictionary(g => g.Key, g => g.ToList());
        }
        
        /// <summary>Get status effect events grouped by target</summary>
        public Dictionary<Entity, List<StatusEffectEvent>> GetStatusEffectsByTarget()
        {
            return GetStatusEffectEvents()
                .Where(e => !e.WasBlocked)
                .GroupBy(e => e.Target)
                .ToDictionary(g => g.Key, g => g.ToList());
        }
        
        /// <summary>Get restoration events grouped by target</summary>
        public Dictionary<Entity, List<RestorationEvent>> GetRestorationByTarget()
        {
            return GetRestorationEvents()
                .GroupBy(e => e.Target)
                .ToDictionary(g => g.Key, g => g.ToList());
        }
        
        /// <summary>Check if this action has any events</summary>
        public bool HasEvents => Events.Count > 0;
        
        /// <summary>Check if this action has any damage events</summary>
        public bool HasDamage => Events.Any(e => e is DamageEvent);
        
        /// <summary>Check if this action has any status effect events</summary>
        public bool HasStatusEffects => Events.Any(e => e is StatusEffectEvent);
        
        /// <summary>Check if this action has any restoration events</summary>
        public bool HasRestoration => Events.Any(e => e is RestorationEvent);
        
        /// <summary>Get the source name for logging</summary>
        public string SourceName => Source?.name ?? "Unknown";
        
        /// <summary>Get the actor's vehicle (if applicable)</summary>
        public Vehicle ActorVehicle => EntityHelpers.GetParentVehicle(Actor);
    }
}
