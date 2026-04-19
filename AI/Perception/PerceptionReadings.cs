using System;
using System.Collections.Generic;

namespace Assets.Scripts.AI.Perception
{
    /// <summary>
    /// Immutable snapshot of all perception signals produced during one pipeline
    /// run. Built by iterating the seat's tracker list; accessed by tracker type
    /// so callers are compile-time safe and no key strings or enum entries are needed.
    /// </summary>
    public class PerceptionReadings
    {
        private readonly Dictionary<Type, float> values = new();

        internal void Set(Type trackerType, float value)
        {
            values[trackerType] = value;
        }

        /// <summary>
        /// Returns the signal for tracker <typeparamref name="T"/>, or
        /// <paramref name="defaultValue"/> if that tracker was not present this run.
        /// </summary>
        public float Get<T>(float defaultValue = 0f) where T : ITracker
        {
            return values.TryGetValue(typeof(T), out float v) ? v : defaultValue;
        }

        /// <summary>All recorded (trackerType, signal) pairs, for logging.</summary>
        public IEnumerable<KeyValuePair<Type, float>> All => values;
    }
}