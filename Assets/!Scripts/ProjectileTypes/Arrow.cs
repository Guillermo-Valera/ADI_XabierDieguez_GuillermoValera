using UnityEngine;

public class Arrow : ParabolicProjectile
{
    private GameObject target;
    private string sourceKey;

    public void Launch(GameObject target, float damage, string sourceKey = null)
    {
        this.target = target;
        this.sourceKey = sourceKey;
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
            unit.Damage(damage, sourceKey);

        Destroy(gameObject);
    }
}
