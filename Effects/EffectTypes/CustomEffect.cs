using System;
using UnityEngine;
using Assets.Scripts.Logging;
using Assets.Scripts.Effects;
using Assets.Scripts.Effects.EffectTypes.CustomEffectCommands;

[Serializable]
public class CustomEffect : EffectBase
{
    [Tooltip("Name/description of this custom effect for logging purposes")]
    public string effectName = "Custom Effect";
    
    [Tooltip("Command to execute (ScriptableObject reference - works in prefabs!)")]
    public EffectCommand command;

    public override void Apply(Entity user, Entity target, EffectContext context, UnityEngine.Object source = null)
    {
        // Prefer command pattern (works with prefabs)
        if (command != null)
        {
            command.Execute(user, target, context, source);

            // Log command execution
            Vehicle vehicle = GetParentVehicle(target);
            string targetName = GetEntityDisplayName(target);
            string userName = GetEntityDisplayName(user);
            string sourceText = source != null ? source.name : "unknown source";
  
            RaceHistory.Log(
                Assets.Scripts.Logging.EventType.SkillUse,
                EventImportance.Debug,
                $"[CUSTOM] {effectName} executed {command.name} by {userName} from {sourceText}",
                vehicle?.currentStage,
                vehicle
            ).WithMetadata("effectName", effectName)
             .WithMetadata("commandName", command.name)
             .WithMetadata("userName", userName)
             .WithMetadata("targetName", targetName)
             .WithMetadata("source", sourceText);
        }
    }
}

