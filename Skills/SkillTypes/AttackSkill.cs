using UnityEngine;

// Preset for attacks
[CreateAssetMenu(menuName = "Racing/Skill/Attack")]
public class AttackSkill : Skill
{
    private void Reset()
    {
        // Set default roll configuration for attacks
        skillRollType = SkillRollType.AttackRoll;
        
        // Only auto-populate if the list is empty or null
        // Allow users to add multiple effects without them being deleted
        if (effectInvocations == null || effectInvocations.Count == 0)
        {
            effectInvocations = new System.Collections.Generic.List<EffectInvocation>
            {
                new EffectInvocation
                {
                    effect = new DamageEffect
                    {
                        formula = new DamageFormula
                        {
                            mode = DamageMode.BaseOnly,
                            baseDice = 1,
                            dieSize = 6,
                            bonus = 0,
                            damageType = DamageType.Physical
                        }
                    },
                    target = EffectTarget.SelectedTarget
                }
            };
        }
    }
}
