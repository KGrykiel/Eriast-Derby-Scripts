using UnityEngine;

[CreateAssetMenu(menuName = "Racing/Skill/Buff")]
public class BuffSkill : Skill
{
    private void OnEnable()
    {
        // Set default: buff skills typically don't require attack rolls
        requiresAttackRoll = false;
        rollType = RollType.None;
        
        // Only auto-populate if the list is empty or null
        if (effectInvocations == null || effectInvocations.Count == 0)
        {
            effectInvocations = new System.Collections.Generic.List<EffectInvocation>
            {
                new EffectInvocation
                {
                    effect = new AttributeModifierEffect(),
                    targetMode = EffectTargetMode.User
                }
            };
        }
    }
}
