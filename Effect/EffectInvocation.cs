using SerializeReferenceEditor;
using UnityEngine;

/// <summary>
/// Simplified EffectInvocation - just wraps an effect with target mode.
/// Roll logic has been moved to Skill level.
/// </summary>
[System.Serializable]
public class EffectInvocation
{
    [SerializeReference, SR]
    public IEffect effect;

    [Tooltip("Who receives this effect? (User, Target, Both, or All in Stage)")]
    public EffectTargetMode targetMode = EffectTargetMode.Target;
}

public enum EffectTargetMode
{
    User,
    Target,
    Both,
    AllInStage
}
