namespace Assets.Scripts.Skills
{
    /// <summary>
    /// Marker subtype for skills that fire the weapon's physical attack mechanism.
    /// Being this type makes the skill ammo-eligible: if a compatible AmmunitionType is loaded,
    /// its onHitNode fires after a successful attack and one charge is consumed.
    /// </summary>
    public class WeaponAttackSkill : Skill
    {
    }
}
