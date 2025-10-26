using UnityEngine;

[CreateAssetMenu(menuName = "Racing/Skill/Special")]
public class SpecialSkill : Skill
{
    private void OnEnable()
    {
        // If the effectInvocations list is empty or not a single CustomEffect, auto-populate for convenience
        if (effectInvocations == null || effectInvocations.Count != 1 || !(effectInvocations[0].effect is CustomEffect))
        {
            effectInvocations = new System.Collections.Generic.List<EffectInvocation>
            {
                new EffectInvocation
                {
                    effect = new CustomEffect(),
                    targetMode = EffectTargetMode.User,
                    requiresRollToHit = false,
                    rollType = RollType.None
                }
            };
        }
    }
}
