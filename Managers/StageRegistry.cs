using System.Collections.Generic;
using Assets.Scripts.Stages;

namespace Assets.Scripts.Managers
{
    /// <summary>
    /// Static registry for runtime stage discovery.
    /// Stages self-register on OnEnable and deregister on OnDisable.
    /// Mirrors VehicleRegistry for consistency.
    /// </summary>
    public static class StageRegistry
    {
        private static readonly List<Stage> _stages = new();

        public static void Register(Stage stage)
        {
            if (!_stages.Contains(stage))
                _stages.Add(stage);
        }

        public static void Unregister(Stage stage) => _stages.Remove(stage);

        /// <summary>Returns a snapshot of all currently registered stages.</summary>
        public static List<Stage> GetAll() => new(_stages);
    }
}
