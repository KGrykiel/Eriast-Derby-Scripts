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
                        formula = new DamageFormula
                        {
                            mode = SkillDamageMode.SkillOnly,
                            skillDice = 1,
                            skillDieSize = 6,
                            skillBonus = 0,
                            skillDamageType = DamageType.Physical
                        }
                    },
                    targetMode = EffectTargetMode.Target,
                    requiresRollToHit = true,
                    rollType = RollType.ArmorClass
                }
            };
        }
    }
}
