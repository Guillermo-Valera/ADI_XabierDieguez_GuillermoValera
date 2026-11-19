using UnityEngine;
using UnityEngine.UI;

public class DamagePopup : MonoBehaviour
{
    private const float Lifetime = 0.9f;
    private const float RiseSpeed = 1.4f;

    private Vector3 worldPos;
    private Text text;
    private Color baseColor;
    private float age;

    public void Init(Vector3 pos, Text t)
    {
        worldPos = pos;
        text = t;
        baseColor = t.color;
        Place();
    }

    private void Update()
    {
        age += Time.deltaTime;
        worldPos += Vector3.up * (RiseSpeed * Time.deltaTime);
        Place();

        float k = Mathf.Clamp01(age / Lifetime);
        text.color = new Color(baseColor.r, baseColor.g, baseColor.b, 1f - k * k);
        transform.localScale = Vector3.one * (k < 0.15f ? Mathf.Lerp(1.4f, 1f, k / 0.15f) : 1f);

        if (age >= Lifetime) Destroy(gameObject);
    }

    private void Place()
    {
        var cam = Camera.main;
        if (cam == null) return;
        transform.position = cam.WorldToScreenPoint(worldPos);
    }
}
