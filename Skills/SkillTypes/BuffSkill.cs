using UnityEngine;

// Preset for buff skills (AttributeModifierEffect)
[CreateAssetMenu(menuName = "Racing/Skill/Buff")]
public class BuffSkill : Skill
{
    private void Reset()
    {
        // Set default configuration for buff skills
        skillRollType = SkillRollType.None;  // Buffs don't require rolls
        
        if (effectInvocations == null || effectInvocations.Count == 0)
        {
            effectInvocations = new System.Collections.Generic.List<EffectInvocation>
            {
                new EffectInvocation
                {
                    effect = new AttributeModifierEffect(),
                    target = EffectTarget.SourceVehicle  // Routes based on attribute type
                }
            };
        }
    }
}
