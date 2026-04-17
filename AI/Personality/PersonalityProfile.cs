using System;
using SerializeReferenceEditor;

namespace Assets.Scripts.AI.Personality
{
    /// <summary>
    /// Data-only personality weights used by the Scoring stage to modulate how
    /// sensitive a seat is to each command axis. Applied as multipliers on the
    /// command weight vector — never inside Perception.
    ///
    /// All standard presets (Ruthless, Honourable, Cunning, Reckless, Defensive)
    /// are instances of this base class with different weights. Subclass only
    /// when a personality requires bespoke data (e.g. a grudge target).
    /// </summary>
    [Serializable]
    [SRName("Personality/Base")]
    public class PersonalityProfile
    {
        /// <summary>How strongly attack-axis signals drive offensive skill selection.</summary>
        public float aggression = 1f;

        /// <summary>How strongly low own-HP and low own-energy drive the heal axis.</summary>
        public float defensiveness = 1f;

        /// <summary>How strongly threat signals drive the flee axis.</summary>
        public float caution = 1f;

        /// <summary>
        /// Tolerance for low-probability / high-variance actions. Higher values reduce the
        /// penalty applied by risk-aware scoring. Risk weighting itself is deferred — this
        /// field is wired up now so designer data stays stable across later refinements.
        /// </summary>
        public float riskTolerance = 1f;

        // === Presets ===
        public static PersonalityProfile Ruthless() => new()
        {
            aggression = 1.6f, defensiveness = 0.5f, caution = 0.4f, riskTolerance = 1.2f
        };

        public static PersonalityProfile Honourable() => new()
        {
            aggression = 0.8f, defensiveness = 1.2f, caution = 1f, riskTolerance = 0.9f
        };

        public static PersonalityProfile Cunning() => new()
        {
            aggression = 1f, defensiveness = 1f, caution = 1.3f, riskTolerance = 0.6f
        };

        public static PersonalityProfile Reckless() => new()
        {
            aggression = 1.4f, defensiveness = 0.7f, caution = 0.3f, riskTolerance = 1.8f
        };

        public static PersonalityProfile Defensive() => new()
        {
            aggression = 0.6f, defensiveness = 1.6f, caution = 1.4f, riskTolerance = 0.7f
        };
    }
}
