using System.Collections.Generic;
using UnityEngine;

public class Bomb : ParabolicProjectile
{
    [SerializeField] private float explosionRadius = 2f;
    [SerializeField] private GameObject explosionVfx;

    private LayerMask affectedLayers;

    public void Launch(Vector2 targetPosition, float damage, LayerMask affectedLayers)
    {
        this.affectedLayers = affectedLayers;
        BeginFlight(targetPosition, damage);
    }

    protected override void OnArrive()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(endPos, explosionRadius, affectedLayers);

        var damaged = new HashSet<Unit>();
        foreach (var col in hits)
        {
            if (col.isTrigger) continue;
            if (col.TryGetComponent(out Unit unit) && damaged.Add(unit))
                unit.Damage(damage);
        }

        if (explosionVfx != null)
            Instantiate(explosionVfx, endPos, Quaternion.identity);

        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(Application.isPlaying ? (Vector3)endPos : transform.position, explosionRadius);
    }
}
