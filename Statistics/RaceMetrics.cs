using System.Collections.Generic;

namespace Assets.Scripts.Statistics
{
    /// <summary>Race-wide statistics snapshot. Pure data -- no logic.</summary>
    public class RaceMetrics
    {
        // ==================== RACE STATE ====================

        public int CurrentRound;
        public int VehiclesTotal;
        public int VehiclesActive;
        public int VehiclesFinished;
        public int VehiclesEliminated;

        // ==================== FINISHING ORDER / ELIMINATIONS ====================

        public List<(string Name, int Position, int Round)> FinishOrder = new();
        public List<(string Name, string Stage, int Round)> Eliminations = new();

        // ==================== AGGREGATE COMBAT ====================

        public int TotalAttacks;
        public int TotalHits;
        public int TotalMisses;
        public int TotalNat20s;
        public int TotalNat1s;
        public int TotalDamageDealt;

        // ==================== AGGREGATE SAVING THROWS ====================

        public int TotalSavingThrows;
        public int SavesPassed;
        public int SavesFailed;

        // ==================== AGGREGATE SKILL / CONDITION USAGE ====================

        /// <summary>Race-wide skill and ability use counts, keyed by name.</summary>
        public Dictionary<string, int> SkillUseCounts = new();

        /// <summary>Race-wide condition application counts, keyed by condition name.</summary>
        public Dictionary<string, int> ConditionApplyCounts = new();

        // ==================== PER-VEHICLE ====================

        /// <summary>Per-vehicle breakdown, keyed by vehicle name.</summary>
        public Dictionary<string, VehicleMetrics> VehicleStats = new();
    }
}
