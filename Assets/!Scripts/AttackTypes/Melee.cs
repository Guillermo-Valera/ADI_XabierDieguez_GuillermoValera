using UnityEngine;

public class Melee : Unit
{
    public override void Attack(Unit target)
    {
        PlayImpactSfx();
        target.Damage(damage);
    }
}
