using UnityEngine;

namespace Assets.Scripts.AI.Perception
{
    /// <summary>
    /// Answers: "How good is my attack window?"
    /// Rises with enemy presence in the same stage and falls as own health
    /// drops — wounded crews are less opportunistic. Personality-agnostic.
    /// </summary>
    public class OpportunityTracker : ITracker
    {
        public float Evaluate(VehicleAISharedContext context)
        {
            if (context == null || context.Self == null) return 0f;

            int enemyCount = context.EnemiesInStage != null ? context.EnemiesInStage.Count : 0;
            if (enemyCount == 0) return 0f;

            float enemyPresence = Mathf.Clamp01(enemyCount / 2f);
            float healthConfidence = Mathf.Clamp01(context.ChassisHealthPercent);
            return Mathf.Clamp01(0.7f * enemyPresence + 0.3f * healthConfidence);
        }
    }
}
