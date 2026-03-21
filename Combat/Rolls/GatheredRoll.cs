using System.Collections.Generic;
using Assets.Scripts.Combat.Rolls.Advantage;

namespace Assets.Scripts.Combat.Rolls
{
    /// <summary>
    /// All data needed to compute a d20 roll, gathered from various sources.
    /// Context Object pattern — eliminates nullable params in calculators.
    /// </summary>
    public readonly struct GatheredRoll
    {
        public readonly List<RollBonus> Bonuses;
        public readonly List<AdvantageSource> AdvantageSources;

        public GatheredRoll(List<RollBonus> bonuses, List<AdvantageSource> advantageSources)
        {
            Bonuses = bonuses ?? new List<RollBonus>();
            AdvantageSources = advantageSources ?? new List<AdvantageSource>();
        }
    }
}
