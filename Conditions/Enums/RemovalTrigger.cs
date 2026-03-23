namespace Assets.Scripts.Conditions
{
    [System.Flags]
    public enum RemovalTrigger
    {
        None = 0,

        OnTurnStart = 1 << 0,
        OnTurnEnd = 1 << 1,
        OnRoundEnd = 1 << 2,
        OnStageExit = 1 << 3,

        OnDamageTaken = 1 << 4,
        OnAttackMade = 1 << 5,
        OnSkillUsed = 1 << 6,
        OnD20Roll = 1 << 7,
        OnMovement = 1 << 8,

        OnSourceDeath = 1 << 9
    }
}
