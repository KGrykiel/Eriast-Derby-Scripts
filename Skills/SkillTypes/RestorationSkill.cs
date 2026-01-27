using UnityEngine;

// Preset for restoration skills (ResourceRestorationEffect)
[CreateAssetMenu(menuName = "Racing/Skill/Restoration")]
public class RestorationSkill : Skill
{
    private void Reset()
    {
        // Set default configuration for restoration skills
        skillRollType = SkillRollType.None;  // Heals don't require rolls
        
        if (effectInvocations == null || effectInvocations.Count == 0)
        {
            effectInvocations = new System.Collections.Generic.List<EffectInvocation>
            {
                new() {
                    effect = new ResourceRestorationEffect(),
                    target = EffectTarget.SourceVehicle  // Routes to chassis for healing
                }
            };
        }
    }
}
