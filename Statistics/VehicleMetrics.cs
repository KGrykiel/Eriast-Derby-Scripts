using System.Collections.Generic;

namespace Assets.Scripts.Statistics
{
    /// <summary>Per-vehicle statistics snapshot. Pure data -- no logic.</summary>
    public class VehicleMetrics
    {
        public string VehicleName;

        // ==================== COMBAT ====================

        public int Attacks;
        public int Hits;
        public int Misses;
        public int Nat20s;
        public int Nat1s;

        // ==================== DAMAGE ====================

        public int DamageDealt;
        public int DamageReceived;

        // ==================== SAVING THROWS ====================

        public int SavingThrows;
        public int SavesPassed;
        public int SavesFailed;

        // ==================== CONDITIONS ====================

        public int ConditionsInflicted;
        public int ConditionsReceived;
        public Dictionary<string, int> ConditionsInflictedByName = new();

        // ==================== SKILL USAGE ====================

        /// <summary>Skills and abilities this vehicle used, by name.</summary>
        public Dictionary<string, int> SkillUseCounts = new();
    }
}
