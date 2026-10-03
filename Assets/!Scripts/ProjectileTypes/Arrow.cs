using UnityEngine;

public class Arrow : ParabolicProjectile
{
    private GameObject target;

    public void Launch(GameObject target, float damage)
    {
        this.target = target;
        BeginFlight(target.transform.position, damage);
    }

    protected override void UpdateTarget()
    {
        if (target != null)
            endPos = target.transform.position;
    }

    protected override void OnArrive()
    {
        if (target != null && target.TryGetComponent(out Unit unit))
            unit.Damage(damage);

        Destroy(gameObject);
    }
}
