using System;
using UnityEngine;
using Assets.Scripts.Effects;
using Assets.Scripts.Effects.EffectTypes.CustomEffectCommands;

[Serializable]
public class CustomEffect : EffectBase
{
    [Tooltip("Name/description of this custom effect for logging purposes")]
    public string effectName = "Custom Effect";
    
    [Tooltip("Command to execute (ScriptableObject reference - works in prefabs!)")]
    public EffectCommand command;
    
    [Header("Command Parameters (Optional)")]
    [Tooltip("Integer parameter passed to command (e.g., speed percent 0-100, -1 = unused). INTEGER-FIRST.")]
    [Range(-1, 100)]
    public int intParameter = -1;

    public override void Apply(Entity user, Entity target, EffectContext context, UnityEngine.Object source = null)
    {
        // Prefer command pattern (works with prefabs)
        if (command != null)
        {
            // Pass CustomEffect as source so command can read floatParameter
            command.Execute(user, target, context, this);
        }
    }
}

