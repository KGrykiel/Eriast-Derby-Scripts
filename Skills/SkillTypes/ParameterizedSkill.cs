using UnityEngine;

/// <summary>
/// Skill variant that can pass runtime parameters to EffectCommands.
/// Use this for skills like "Set Speed" where the same command logic
/// is reused with different parameter values.
/// </summary>
[CreateAssetMenu(fileName = "New Parameterized Skill", menuName = "Racing/Skill/Parameterized")]
public class ParameterizedSkill : Skill
{
    [Header("Command Parameters")]
    [Tooltip("Float parameter passed to commands (e.g., target speed 0-1)")]
    [Range(0f, 1f)]
    public float floatParameter = 1.0f;
}

