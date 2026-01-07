using UnityEngine;

[CreateAssetMenu(menuName = "Racing/Skill/Restoration")]
public class RestorationSkill : Skill
{
    private void OnEnable()
    {
        // Set default: restoration skills typically don't require attack rolls
        requiresAttackRoll = false;
        rollType = RollType.None;
        
        // Only auto-populate if the list is empty or null
        if (effectInvocations == null || effectInvocations.Count == 0)
        {
            effectInvocations = new System.Collections.Generic.List<EffectInvocation>
            {
                new EffectInvocation
                {
                    effect = new ResourceRestorationEffect(),
                    targetMode = EffectTargetMode.User
                }
            };
        }
    }
}
