using UnityEngine;

namespace Assets.Scripts.Effects.EffectTypes.CustomEffectCommands
{
    /// <summary>Sets target speed percentage on the source vehicle's drive component.</summary>
    [CreateAssetMenu(fileName = "SetSpeed", menuName = "Racing/Commands/Set Speed")]
    public class SetSpeedCommand : EffectCommand
    {
        [Tooltip("Default target speed percentage (0-100) if skill is not a ParameterizedSkill")]
        [Range(0, 100)]
        public int defaultTargetSpeedPercent = 100;

        public override void Execute(Entity target, EffectContext context, object source)
        {
            // Target entity should be the DriveComponent (self-targeting skill)
            DriveComponent drive = target as DriveComponent;
            if (drive == null)
            {
                Debug.LogWarning("[SetSpeedCommand] Target is not a DriveComponent!");
                return;
            }

            // Try to read speed from CustomEffect.intParameter
            int speedPercent = defaultTargetSpeedPercent;
            
            if (source is CustomEffect customEffect && customEffect.intParameter >= 0)
            {
                speedPercent = customEffect.intParameter;
            }

            // Set the target speed (integer percentage)
            drive.SetTargetSpeed(speedPercent);
        }
    }
}

