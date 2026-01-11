using System.Collections.Generic;
using System.Linq;

namespace Assets.Scripts.Combat.Damage
{
    /// <summary>
    /// Complete result of a damage calculation.
    /// Pure data class - use DamageCalculator for logic, DamageResultFormatter for display.
    /// </summary>
    [System.Serializable]
    public class DamageResult
    {
        /// <summary>Type of damage (Fire, Physical, etc.)</summary>
        public DamageType damageType;
        
        /// <summary>All damage sources (weapon dice, skill dice, etc.)</summary>
        public List<DamageSourceEntry> sources;
        
        /// <summary>Resistance applied to this damage</summary>
        public ResistanceLevel resistanceLevel;
        
        /// <summary>Final damage after resistance</summary>
        public int finalDamage;
        
        /// <summary>
        /// Raw total before resistance (calculated from sources).
        /// </summary>
        public int RawTotal => sources?.Sum(s => s.Total) ?? 0;
        
        public DamageResult()
        {
            sources = new List<DamageSourceEntry>();
            resistanceLevel = ResistanceLevel.Normal;
        }
        
        /// <summary>
        /// Create a new damage result for a specific damage type.
        /// </summary>
        public static DamageResult Create(DamageType type)
        {
            return new DamageResult
            {
                damageType = type,
                sources = new List<DamageSourceEntry>(),
                resistanceLevel = ResistanceLevel.Normal,
                finalDamage = 0
            };
        }
    }
}
