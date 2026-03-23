namespace Assets.Scripts.Conditions
{
    [System.Flags]
    public enum ConditionCategory
    {
        None = 0,
        
        Buff = 1 << 0,
        Debuff = 1 << 1,
        
        DoT = 1 << 2,
        HoT = 1 << 3,
        
        CrowdControl = 1 << 4,
        AttributeModifier = 1 << 5
    }
}
