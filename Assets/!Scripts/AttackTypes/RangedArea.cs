using UnityEngine;

public class RangedArea : Unit
{
    [SerializeField] private Bomb bombPrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private bool friendlyFire;

    public override void Attack(Unit target)
    {
        Bomb bomb = Instantiate(bombPrefab, firePoint.position, Quaternion.identity);

        LayerMask mask = enemyLayer;
        if (friendlyFire)
            mask |= 1 << gameObject.layer;   // añade también tu propio bando

        bomb.Launch(target.transform.position, damage, mask);
    }
}
