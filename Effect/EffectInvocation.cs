//using SerializeReferenceEditor;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class EffectInvocation
{
    [SerializeReference]
    public IEffect effect;

    public EffectTargetMode targetMode = EffectTargetMode.Target;
    public bool requiresRollToHit = false;
    public RollType rollType = RollType.ArmorClass;

    public void Apply(Entity user, Entity mainTarget, Stage context, Object source, int toHitBonus = 0)
    {
        if (effect == null) return;

        List<Entity> targets = new List<Entity>();
        switch (targetMode)
        {
            case EffectTargetMode.User:
                targets.Add(user);
                break;
            case EffectTargetMode.Target:
                targets.Add(mainTarget);
                break;
            case EffectTargetMode.Both:
                targets.Add(user);
                targets.Add(mainTarget);
                break;
            case EffectTargetMode.AllInStage:
                if (user is Vehicle vehicleUser && vehicleUser.currentStage != null)
                    targets.AddRange(vehicleUser.currentStage.vehiclesInStage.FindAll(v => v != user));
                break;
        }

        foreach (var target in targets)
        {
            bool apply = true;
            if (requiresRollToHit)
            {
                if (!RollUtility.RollToHit(user as Vehicle, target, rollType, toHitBonus, source?.ToString()))
                {
                    SimulationLogger.LogEvent($"{(user is Vehicle vu ? vu.vehicleName : user.name)} missed {(target is Vehicle vt ? vt.vehicleName : target.name)} with effect.");
                    apply = false;
                }
            }
            if (apply)
                effect.Apply(user, target, context, source);
        }
    }
}

public enum EffectTargetMode
{
    User,
    Target,
    Both,
    AllInStage
    // Extend as needed
}
