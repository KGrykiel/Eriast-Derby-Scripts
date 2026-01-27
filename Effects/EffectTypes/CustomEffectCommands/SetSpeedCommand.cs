using UnityEngine;

namespace Assets.Scripts.Effects.EffectTypes.CustomEffectCommands
{
    /// <summary>
    /// Sets the target speed (as proportion 0-1) on the source vehicle's drive component.
    /// Used by driver skills to control vehicle speed during action phase.
    /// </summary>
    [CreateAssetMenu(fileName = "SetSpeed", menuName = "Racing/Commands/Set Speed")]
    public class SetSpeedCommand : EffectCommand
    {
        [Tooltip("Target speed as proportion of maxSpeed (0.0 = stop, 0.5 = half speed, 1.0 = full speed)")]
        [Range(0f, 1.0f)]
        public float targetSpeed = 1.0f;

        public override void Execute(Entity user, Entity target, EffectContext context, Object source)
        {
            // Target entity should be the DriveComponent (self-targeting skill)
            Debug.Log("[SetSpeedCommand] Executing SetSpeedCommand on target: " + target.GetDisplayName());
            DriveComponent drive = target as DriveComponent;
            if (drive == null)
            {
                Debug.LogWarning("[SetSpeedCommand] Target is not a DriveComponent!");
                return;
            }

            // Set the target speed (proportional)
            drive.SetTargetSpeed(targetSpeed);
        }
    }
}

