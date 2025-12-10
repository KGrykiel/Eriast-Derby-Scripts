using System;
using UnityEngine;
using UnityEngine.Events;
using RacingGame.Events;

[Serializable]
public class CustomEffect : EffectBase
{
    [Tooltip("Name/description of this custom effect for logging purposes")]
    public string effectName = "Custom Effect";
    
    // This UnityEvent can be set up in the Inspector to call any method with these parameters.
    public UnityEvent<Entity, Entity> specialEvent;

    public override void Apply(Entity user, Entity target, UnityEngine.Object context = null, UnityEngine.Object source = null)
    {
        if (specialEvent != null)
        {
            specialEvent.Invoke(user, target);

            // Log custom effect invocation
            Vehicle vehicle = GetParentVehicle(target);
            string targetName = GetEntityDisplayName(target);
            string userName = GetEntityDisplayName(user);
            string sourceText = source != null ? source.name : "unknown source";
  
            RaceHistory.Log(
                RacingGame.Events.EventType.SkillUse,
                EventImportance.Debug,
                $"[CUSTOM] {effectName} triggered by {userName} on {targetName} from {sourceText}",
                vehicle?.currentStage,
                vehicle
            ).WithMetadata("effectName", effectName)
             .WithMetadata("userName", userName)
             .WithMetadata("targetName", targetName)
             .WithMetadata("source", sourceText)
             .WithMetadata("listenerCount", specialEvent.GetPersistentEventCount());
        }
    }
}
