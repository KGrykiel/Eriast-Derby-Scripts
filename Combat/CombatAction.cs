using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets.Scripts.Combat
{
    /// <summary>
    /// Groups all events from a single combat action for aggregated logging.
    /// e.g. a skill dealing 1d8 Physical + 2d6 Fire = 1 action, 2 DamageEvents.
    /// </summary>
    public class CombatAction
    {
        public Entity Actor { get; set; }
        public Object Source { get; set; }
        public Vehicle PrimaryTarget { get; set; }

        /// <summary>Stored, not derived from Actor. Null for standalone entities.</summary>
        public Vehicle SourceVehicle { get; set; }

        /// <summary>Null for component-only or standalone actions. Enables "CharacterName uses Skill" logging.</summary>
        public Character SourceCharacter { get; set; }

        public List<CombatEvent> Events { get; } = new List<CombatEvent>();

        public CombatAction(
            Entity actor,
            Object source,
            Vehicle primaryTarget = null,
            Vehicle sourceVehicle = null,
            Character sourceCharacter = null)
        {
            Actor = actor;
            Source = source;
            PrimaryTarget = primaryTarget;
            SourceVehicle = sourceVehicle;
            SourceCharacter = sourceCharacter;
        }

        public void AddEvent(CombatEvent evt)
        {
            Events.Add(evt);
        }

        // ==================== QUERY HELPERS ====================

        public IEnumerable<DamageEvent> GetDamageEvents() 
            => Events.OfType<DamageEvent>();

        public IEnumerable<StatusEffectEvent> GetStatusEffectEvents() 
            => Events.OfType<StatusEffectEvent>();

        public IEnumerable<RestorationEvent> GetRestorationEvents() 
            => Events.OfType<RestorationEvent>();

        public IEnumerable<AttackRollEvent> GetAttackRollEvents() 
            => Events.OfType<AttackRollEvent>();

        public IEnumerable<SavingThrowEvent> GetSavingThrowEvents() 
            => Events.OfType<SavingThrowEvent>();

        public IEnumerable<SkillCheckEvent> GetSkillCheckEvents() 
            => Events.OfType<SkillCheckEvent>();

        public Dictionary<Entity, List<DamageEvent>> GetDamageByTarget()
        {
            return GetDamageEvents()
                .GroupBy(e => e.Target)
                .ToDictionary(g => g.Key, g => g.ToList());
        }

        public Dictionary<Entity, List<RestorationEvent>> GetRestorationByTarget()
        {
            return GetRestorationEvents()
                .GroupBy(e => e.Target)
                .ToDictionary(g => g.Key, g => g.ToList());
        }

        public bool HasEvents => Events.Count > 0;
        public string SourceName => Source != null ? (Source.name ?? "Unknown") : "Unknown";

        /// <summary>Prefers stored SourceVehicle, falls back to entity derivation.</summary>
        public Vehicle ActorVehicle => SourceVehicle ?? EntityHelpers.GetParentVehicle(Actor);
    }
}
