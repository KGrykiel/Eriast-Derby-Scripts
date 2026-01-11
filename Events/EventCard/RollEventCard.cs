using UnityEngine;
using System.Collections.Generic;
using Assets.Scripts.Combat.Attacks;

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
        // Perform skill check using AttackCalculator
        var result = AttackCalculator.PerformSkillCheck(
            vehicle.chassis, 
            difficulty, 
            $"Event: {conditionDescription}");

        if (result.success == true)
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
