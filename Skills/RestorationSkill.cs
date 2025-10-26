using UnityEngine;

[CreateAssetMenu(menuName = "Racing/Skill/Restoration")]
public class RestorationSkill : Skill
{
    private void OnEnable()
    {

        // If the effectInvocations list is empty or not a single ResourceRestorationEffect, auto-populate for convenience
        if (effectInvocations == null || effectInvocations.Count != 1 || !(effectInvocations[0].effect is ResourceRestorationEffect))
        {
            effectInvocations = new System.Collections.Generic.List<EffectInvocation>
            {
                new EffectInvocation
                {
                    effect = new ResourceRestorationEffect(),
                    targetMode = EffectTargetMode.User,
                    requiresRollToHit = false,
                    rollType = RollType.None
                }
            };
        }
    }
}
