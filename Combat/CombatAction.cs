using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Combat.Rolls;

namespace Assets.Scripts.Combat
{
    /// <summary>
    /// Groups all events from a single combat action for aggregated logging.
    /// e.g. a skill dealing 1d8 Physical + 2d6 Fire = 1 action, 2 DamageEvents.
    /// </summary>
    public class CombatAction
    {
        public RollActor SourceActor { get; set; }
        public string Source { get; set; }
        public Vehicle PrimaryTarget { get; set; }

        /// <summary>Stored, not derived from Actor. Null for standalone entities.</summary>
        public Vehicle SourceVehicle { get; set; }

        /// <summary>Derived from SourceActor for generic event handling.</summary>
        public Entity Actor => SourceActor?.GetEntity();

        public List<CombatEvent> Events { get; } = new List<CombatEvent>();

        public CombatAction(
            RollActor sourceActor,
            string source,
            Vehicle primaryTarget = null,
            Vehicle sourceVehicle = null)
        {
            SourceActor = sourceActor;
            Source = source;
            PrimaryTarget = primaryTarget;
            SourceVehicle = sourceVehicle;
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

        public IEnumerable<StatusEffectExpiredEvent> GetStatusExpiredEvents()
            => Events.OfType<StatusEffectExpiredEvent>();

        public IEnumerable<StatusEffectRefreshedEvent> GetStatusRefreshedEvents()
            => Events.OfType<StatusEffectRefreshedEvent>();

        public IEnumerable<StatusEffectIgnoredEvent> GetStatusIgnoredEvents()
            => Events.OfType<StatusEffectIgnoredEvent>();

        public IEnumerable<StatusEffectReplacedEvent> GetStatusReplacedEvents()
            => Events.OfType<StatusEffectReplacedEvent>();

        public IEnumerable<StatusEffectKeptStrongerEvent> GetStatusKeptStrongerEvents()
            => Events.OfType<StatusEffectKeptStrongerEvent>();

        public IEnumerable<StatusEffectStackLimitEvent> GetStatusStackLimitEvents()
            => Events.OfType<StatusEffectStackLimitEvent>();

        public IEnumerable<RestorationEvent> GetRestorationEvents()
            => Events.OfType<RestorationEvent>();

        public IEnumerable<AttackRollEvent> GetAttackRollEvents() 
            => Events.OfType<AttackRollEvent>();

        public IEnumerable<SavingThrowEvent> GetSavingThrowEvents() 
            => Events.OfType<SavingThrowEvent>();

        public IEnumerable<SkillCheckEvent> GetSkillCheckEvents() 
            => Events.OfType<SkillCheckEvent>();

        public IEnumerable<OpposedCheckEvent> GetOpposedCheckEvents()
            => Events.OfType<OpposedCheckEvent>();

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
        public string SourceName => Source ?? "Unknown";

        /// <summary>Prefers stored SourceVehicle, falls back to entity derivation.</summary>
        public Vehicle ActorVehicle => SourceVehicle ?? EntityHelpers.GetParentVehicle(Actor);
    }
}
