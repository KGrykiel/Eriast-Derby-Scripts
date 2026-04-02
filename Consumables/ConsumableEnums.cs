using System;

namespace Assets.Scripts.Consumables
{
    [Flags]
    public enum ConsumableAccess
    {
        None = 0,
        Combat = 1,
        Utility = 2
    }
}
