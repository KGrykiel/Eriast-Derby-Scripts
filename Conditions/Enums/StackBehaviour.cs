using System;
using SerializeReferenceEditor;
using UnityEngine;

namespace Assets.Scripts.Conditions
{
    /// <summary>
    /// Determines how a condition behaves when reapplied to a target that already has it active.
    /// Each concrete type carries only the data it needs for that behaviour.
    /// </summary>
    public interface IStackingBehaviour { }

    /// <summary>Reset duration to base, keep a single instance.</summary>
    [Serializable]
    [SRName("Refresh")]
    public class RefreshStacking : IStackingBehaviour { }

    /// <summary>Create a new instance per application, up to maxStacks (0 = unlimited).</summary>
    [Serializable]
    [SRName("Stack")]
    public class StackStacking : IStackingBehaviour
    {
        [Tooltip("Maximum concurrent stacks. 0 = unlimited.")]
        public int maxStacks = 0;
    }

    /// <summary>Do nothing if already active — duration does not refresh.</summary>
    [Serializable]
    [SRName("Ignore")]
    public class IgnoreStacking : IStackingBehaviour { }

    /// <summary>Replace the existing instance if the new application is stronger.</summary>
    [Serializable]
    [SRName("Replace")]
    public class ReplaceStacking : IStackingBehaviour { }
}
