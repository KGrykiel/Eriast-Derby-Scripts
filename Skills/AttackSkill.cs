using UnityEngine;

// Preset for attacks
[CreateAssetMenu(menuName = "Racing/Skill/Attack")]
public class AttackSkill : Skill
{
    private void OnEnable()
    {
        // If the effectInvocations list is empty or not a single DamageEffect, auto-populate for convenience
        if (effectInvocations == null || effectInvocations.Count != 1 || !(effectInvocations[0].effect is DamageEffect))
        {
            effectInvocations = new System.Collections.Generic.List<EffectInvocation>
            {
                new EffectInvocation
                {
                    effect = new DamageEffect
                    {
                        damageDice = 1,
                        damageDieSize = 6,
                        damageBonus = 0
                    },
                    targetMode = EffectTargetMode.Target,
                    requiresRollToHit = true,
                    rollType = RollType.ArmorClass
                }
            };
        }
    }
}
