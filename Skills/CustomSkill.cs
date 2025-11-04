using UnityEngine;

[CreateAssetMenu(menuName = "Racing/Skill/Custom")]
public class CustomSkill : Skill
{
    private void OnEnable()
    {
        // No presets or effect list modifications; fully configurable in the Inspector.
    }
}
