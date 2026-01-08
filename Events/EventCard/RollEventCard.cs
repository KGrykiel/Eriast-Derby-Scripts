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
        // Build modifiers list (future: add character skill bonuses)
        var modifiers = RollUtility.BuildModifiers()
            // Add vehicle/character bonuses here when implemented
            // .Add("Pilot Skill", vehicle.pilot?.skillBonus ?? 0, vehicle.pilot?.name)
            .Build();

        var breakdown = RollUtility.SkillCheckWithBreakdown(modifiers, difficulty, $"Event: {conditionDescription}");

        if (breakdown.success == true)
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

        // Get the chassis entity (primary target entity for vehicle)
        Entity targetEntity = mainTarget.chassis;
        if (targetEntity == null)
        {
            Debug.LogWarning($"[RollEventCard] Vehicle {mainTarget.vehicleName} has no chassis!");
            return;
        }

        foreach (var invocation in invocations)
        {
            // Apply effect directly (no roll logic needed here)
            if (invocation.effect != null)
            {
                invocation.effect.Apply(targetEntity, targetEntity, stage, this);
            }
        }
    }
}
