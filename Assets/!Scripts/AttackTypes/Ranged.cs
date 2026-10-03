using UnityEngine;

public class Ranged : Unit
{
    [SerializeField] private Arrow arrowPrefab;
    [SerializeField] private Transform firePoint;


    public override void Attack(Unit target)
    {
        // dentro de Attack(), donde antes hacías targets[0].GetComponent<Unit>().Damage(damage):
        Arrow arrow = Instantiate(arrowPrefab, firePoint.position, Quaternion.identity);
        arrow.Launch(target.gameObject, damage);
    }
}
