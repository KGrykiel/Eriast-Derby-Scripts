using System;
using UnityEngine;

namespace Assets.Scripts.Managers.Turn
{
    /// <summary>Event bus for turn lifecycle, operations, and player actions. Used for logging.</summary>
    public static class TurnEventBus
    {
        public static event Action<TurnEvent> OnEvent;

        public static void Emit(TurnEvent evt)
        {
            if (evt == null)
            {
                Debug.LogWarning("[TurnEventBus] Attempted to emit null event!");
                return;
            }

            OnEvent?.Invoke(evt);
        }

        public static void ClearAllSubscribers()
        {
            OnEvent = null;
        }
    }
}
