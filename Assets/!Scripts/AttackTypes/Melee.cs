using UnityEngine;

public class Melee : Unit
{
    public override void Attack(Unit target)
    {
        target.Damage(damage);
    }
}
