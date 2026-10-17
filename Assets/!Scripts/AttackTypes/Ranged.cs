using UnityEngine;

public class Ranged : Unit
{
    [SerializeField] private Arrow arrowPrefab;
    [SerializeField] private Transform firePoint;

    public override void Attack(Unit target)
    {
        Arrow arrow = Instantiate(arrowPrefab, firePoint.position, Quaternion.identity);
        arrow.Launch(target.gameObject, damage);
    }
}
