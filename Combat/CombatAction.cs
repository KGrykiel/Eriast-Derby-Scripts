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
        public List<CombatEvent> Events { get; } = new List<CombatEvent>();

        public void AddEvent(CombatEvent evt)
        {
            Events.Add(evt);
        }

        // ==================== QUERY HELPERS ====================

        public IEnumerable<T> Get<T>() where T : CombatEvent => Events.OfType<T>();

        public IEnumerable<(Entity Target, RollActor Actor, string CausalSource, List<DamageEvent> Events)> GetDamageByTarget()
        {
            return Get<DamageEvent>()
                .GroupBy(e => (e.Target, e.Actor, e.CausalSource))
                .Select(g => (g.Key.Target, g.Key.Actor, g.Key.CausalSource, g.ToList()));
        }

        public IEnumerable<(Entity Target, RollActor Actor, string CausalSource, List<RestorationEvent> Events)> GetRestorationByTarget()
        {
            return Get<RestorationEvent>()
                .GroupBy(e => (e.Target, e.Actor, e.CausalSource))
                .Select(g => (g.Key.Target, g.Key.Actor, g.Key.CausalSource, g.ToList()));
        }

        public bool HasEvents => Events.Count > 0;
    }
}
