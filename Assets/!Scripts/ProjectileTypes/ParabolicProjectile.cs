using UnityEngine;

public abstract class ParabolicProjectile : MonoBehaviour
{
    [SerializeField] protected float speed = 8f;        // unidades/segundo en horizontal
    [SerializeField] protected float arcHeight = 2f;    // altura máxima extra del arco
    [SerializeField] private bool rotateAlongPath = true;
    [SerializeField] private AudioClip impactSfx;       // sonido al llegar
    [SerializeField, Range(0f, 1f)] private float impactVolume = 0.7f;

    protected Vector2 startPos;
    protected Vector2 endPos;
    protected float damage;

    private float duration;
    private float elapsed;
    private Vector2 lastPos;
    private bool launched;

    protected void BeginFlight(Vector2 target, float dmg)
    {
        startPos = transform.position;
        endPos = target;
        damage = dmg;

        float distance = Vector2.Distance(startPos, endPos);
        duration = Mathf.Max(distance / speed, 0.1f);
        elapsed = 0f;
        lastPos = startPos;
        launched = true;
    }

    // Las hijas pueden actualizar endPos aquí (objetivo móvil)
    protected virtual void UpdateTarget() { }

    protected abstract void OnArrive();

    private void Update()
    {
        if (!launched) return;

        UpdateTarget();

        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / duration);

        Vector2 pos = Vector2.Lerp(startPos, endPos, t);
        pos.y += arcHeight * 4f * t * (1f - t);
        transform.position = pos;

        if (rotateAlongPath)
        {
            Vector2 dir = pos - lastPos;
            if (dir.sqrMagnitude > 0.00001f)
            {
                float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0, 0, angle);
            }
        }
        lastPos = pos;

        if (t >= 1f)
        {
            launched = false;
            SfxPlayer.Play(impactSfx, impactVolume);
            OnArrive();
        }
    }
}
