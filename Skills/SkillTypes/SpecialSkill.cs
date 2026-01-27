using UnityEngine;

// Preset for custom/special skills
[CreateAssetMenu(menuName = "Racing/Skill/Special")]
public class SpecialSkill : Skill
{
    private void Reset()
    {
        // Set default configuration for special skills
        skillRollType = SkillRollType.None;  // Default: no roll required
        
        if (effectInvocations == null || effectInvocations.Count == 0)
        {
            effectInvocations = new System.Collections.Generic.List<EffectInvocation>
            {
                new() {
                    effect = new CustomEffect(),
                    target = EffectTarget.SelectedTarget
                }
            };
        }
    }
}
