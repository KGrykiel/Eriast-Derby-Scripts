using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Racing/EventCards/RollEventCard")]
public class RollEventCard : EventCard
{
    public string conditionDescription;
    public int difficulty = 10;

    [SerializeField]
    public List<EffectInvocation> rewardEffects = new List<EffectInvocation>();

    [SerializeField]
    public List<EffectInvocation> penaltyEffects = new List<EffectInvocation>();

    public override void Trigger(Vehicle vehicle, Stage stage)
    {
        // Simple d20 roll vs difficulty for stage events
        // TODO: Replace with SkillCheckCalculator when implemented
        int roll = RollUtility.RollD20();
        bool success = roll >= difficulty;

        if (success)
        {
            ApplyEffectInvocations(rewardEffects, vehicle, stage);
        }
        else
        {
            ApplyEffectInvocations(penaltyEffects, vehicle, stage);
        }
    }

    private void ApplyEffectInvocations(List<EffectInvocation> invocations, Vehicle mainTarget, Stage stage)
    {
        if (invocations == null) return;

        Entity targetEntity = mainTarget.chassis;
        if (targetEntity == null)
        {
            Debug.LogWarning($"[RollEventCard] Vehicle {mainTarget.vehicleName} has no chassis!");
            return;
        }

        foreach (var invocation in invocations)
        {
            if (invocation.effect != null)
            {
                invocation.effect.Apply(targetEntity, targetEntity, stage, this);
            }
        }
    }
}
