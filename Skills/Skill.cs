using UnityEngine;
using System.Collections.Generic;
public abstract class Skill : ScriptableObject
{
    public string description;
    public int energyCost = 1;
    [SerializeField]
    public List<EffectInvocation> effectInvocations = new List<EffectInvocation>();

    // All skills must implement this
    public virtual bool Use(Vehicle user, Vehicle mainTarget)
    {
        if (effectInvocations == null || effectInvocations.Count == 0 || mainTarget == null)
            return false;

        bool anyApplied = false;

        foreach (var invocation in effectInvocations)
        {
            int toHitBonus = GetCasterToHitBonus(user, invocation.rollType);
            invocation.Apply(user, mainTarget, user.currentStage, this, toHitBonus);
            anyApplied = true;
        }

        if (anyApplied)
            SimulationLogger.LogEvent($"{user.vehicleName} used {name}.");

        return anyApplied;
    }
    // Utility: Get caster's to-hit bonus based on roll type
    protected int GetCasterToHitBonus(Vehicle caster, RollType rollType)
    {
        if (rollType == RollType.None)
            return 0; // No bonus for always-hitting skills
        // For now, we assume all vehicles have a to-hit bonus of 0.
        return 0;
    }

}
