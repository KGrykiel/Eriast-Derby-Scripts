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
        int bonus = 0; // will add reference to a skill later.

        var (success, roll, usedBonus, total) = RollUtility.SkillCheck(bonus, difficulty);

        SimulationLogger.LogEvent($"{vehicle.vehicleName} faces event: {description} (Roll: {roll} + {usedBonus} vs {difficulty})");

        if (success)
        {
            ApplyEffectInvocations(rewardEffects, vehicle, stage);
            SimulationLogger.LogEvent($"{vehicle.vehicleName} succeeded! Reward applied.");
        }
        else
        {
            ApplyEffectInvocations(penaltyEffects, vehicle, stage);
            SimulationLogger.LogEvent($"{vehicle.vehicleName} failed! Penalty applied.");
        }
    }


    private void ApplyEffectInvocations(List<EffectInvocation> invocations, Vehicle mainTarget, Stage stage)
    {
        if (invocations == null) return;

        foreach (var invocation in invocations)
        {
            invocation.Apply(mainTarget, mainTarget, stage, this, 0);
        }
    }
}
