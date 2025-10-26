using UnityEngine;

[CreateAssetMenu(menuName = "Racing/Skill/Buff")]
public class BuffSkill : Skill
{
    private void OnEnable()
    {
        // If the effectInvocations list is empty or not a single AttributeModifierEffect, auto-populate for convenience
        if (effectInvocations == null || effectInvocations.Count != 1 || !(effectInvocations[0].effect is AttributeModifierEffect))
        {
            effectInvocations = new System.Collections.Generic.List<EffectInvocation>
            {
                new EffectInvocation
                {
                    effect = new AttributeModifierEffect(),
                    targetMode = EffectTargetMode.User,
                    requiresRollToHit = false,
                    rollType = RollType.None
                }
            };
        }
    }
}
