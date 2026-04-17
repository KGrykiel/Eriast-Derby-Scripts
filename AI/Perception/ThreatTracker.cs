using UnityEngine;

namespace Assets.Scripts.AI.Perception
{
    /// <summary>
    /// Answers: "How threatened am I right now?"
    /// Combines own health deficit (dominant) with enemy pressure in the stage
    /// (secondary). Does not read personality — that is applied later by the
    /// Scoring stage.
    /// </summary>
    public class ThreatTracker : ITracker
    {
        public float Evaluate(VehicleAISharedContext context)
        {
            if (context == null || context.Self == null) return 0f;

            int enemyCount = context.EnemiesInStage != null ? context.EnemiesInStage.Count : 0;
            float enemyPressure = Mathf.Clamp01(enemyCount / 3f);
            float healthDeficit = 1f - Mathf.Clamp01(context.ChassisHealthPercent);

            return Mathf.Clamp01(0.6f * healthDeficit + 0.4f * enemyPressure);
        }
    }
}
