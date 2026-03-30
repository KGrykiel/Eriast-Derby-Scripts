using UnityEngine;
using SerializeReferenceEditor;

namespace Assets.Scripts.Effects
{
    /// <summary>
    /// The full wrapper that is used in the Unity editor. Allows to route each effect in a skill/event to different targets
    /// for example, damage to selected target but speed buff to self. Each effect's Apply handles its own internal routing.
    /// </summary>
    [System.Serializable]
    public class EffectInvocation
    {
        [SerializeReference, SR]
        public IEffect effect;

        [Tooltip("Who receives this effect?")]
        public EffectTarget target = EffectTarget.SelectedTarget;
    }

    /// <summary>
    /// Target of the particular effect.
    /// </summary>
    public enum EffectTarget
    {
        // Single targets - Source
        [Tooltip("The component using this skill (e.g., weapon that fires, power core that overloads)")]
        SourceComponent,

        [Tooltip("The user's vehicle (routes based on effect type: damage?chassis, speed?drive, energy?power core)")]
        SourceVehicle,

        [Tooltip("Player-selected component on user's vehicle (shows component selection UI)")]
        SourceComponentSelection,

        // Single targets - Selected target
        [Tooltip("Player-selected target (respects manual component targeting from UI if used)")]
        SelectedTarget,

        [Tooltip("Target vehicle (routes based on effect type, ignores manual component selection)")]
        TargetVehicle,

        // AoE — Intra-vehicle
        [Tooltip("All components on the target vehicle")]
        AllComponentsOnTarget,

        [Tooltip("One random component on the target vehicle")]
        RandomComponentOnTarget,

        // AoE — Inter-vehicle (lane-scoped; lane is the tactical position unit, not a physical corridor)
        [Tooltip("All vehicles in the same lane as the selected target (each vehicle receives effects via chassis)")]
        AllVehiclesInTargetLane,

        [Tooltip("All vehicles in the same lane as the target, excluding the target vehicle itself")]
        AllOtherVehiclesInTargetLane,

        // AoE — Inter-vehicle (stage-scoped)
        [Tooltip("All other vehicles in the same stage as the target, excluding the target vehicle itself")]
        AllOtherVehiclesInStage,

        // Seat-targeting — character condition effects
        [Tooltip("All seats on the target vehicle")]
        AllSeatsOnTargetVehicle,

        [Tooltip("All seats on the source vehicle")]
        AllSeatsOnSourceVehicle,

        [Tooltip("The seat of the rolling character (ctx.SourceActor). Skipped for non-character actors.")]
        SourceActorSeat,
    }
}