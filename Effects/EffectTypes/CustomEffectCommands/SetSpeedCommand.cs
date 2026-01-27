using UnityEngine;

namespace Assets.Scripts.Effects.EffectTypes.CustomEffectCommands
{
    /// <summary>
    /// Sets the target speed (as proportion 0-1) on the source vehicle's drive component.
    /// Used by driver skills to control vehicle speed during action phase.
    /// Reads speed value from ParameterizedSkill.floatParameter if available.
    /// </summary>
    [CreateAssetMenu(fileName = "SetSpeed", menuName = "Racing/Commands/Set Speed")]
    public class SetSpeedCommand : EffectCommand
    {
        [Tooltip("Default target speed if skill is not a ParameterizedSkill")]
        [Range(0f, 1.0f)]
        public float defaultTargetSpeed = 1.0f;

        public override void Execute(Entity user, Entity target, EffectContext context, object source)
        {
            // Target entity should be the DriveComponent (self-targeting skill)
            DriveComponent drive = target as DriveComponent;
            if (drive == null)
            {
                Debug.LogWarning("[SetSpeedCommand] Target is not a DriveComponent!");
                return;
            }

            // Try to read speed from CustomEffect.floatParameter
            float speedToSet = defaultTargetSpeed;
            
            if (source is CustomEffect customEffect && customEffect.floatParameter >= 0f)
            {
                // Effect provides parameter value
                speedToSet = customEffect.floatParameter;
            }

            // Set the target speed (proportional)
            drive.SetTargetSpeed(speedToSet);
        }
    }
}

