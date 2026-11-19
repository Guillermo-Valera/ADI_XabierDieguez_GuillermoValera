using UnityEngine;
using UnityEngine.UI;

public static class DamagePopups
{
    private static RectTransform container;
    private static Font font;

    public static void Show(Vector3 worldPos, float amount, bool isBase)
    {
        if (container == null && !CreateContainer()) return;

        var go = new GameObject("DamagePopup", typeof(RectTransform), typeof(Text), typeof(Outline));
        go.transform.SetParent(container, false);

        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(200, 60);

        var text = go.GetComponent<Text>();
        text.font = font;
        text.fontSize = isBase ? 40 : 30;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.raycastTarget = false;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.color = isBase ? new Color(1f, 0.45f, 0.2f) : new Color(1f, 0.92f, 0.3f);
        text.text = "-" + amount.ToString("0.##");

        var outline = go.GetComponent<Outline>();
        outline.effectColor = new Color(0, 0, 0, 0.9f);
        outline.effectDistance = new Vector2(2, -2);

        var popup = go.AddComponent<DamagePopup>();
        popup.Init(worldPos + new Vector3(Random.Range(-0.3f, 0.3f), isBase ? 2.2f : 1.1f, 0), text);
    }

    private static bool CreateContainer()
    {
        var hud = GameObject.Find("HUD");
        Canvas canvas = hud != null ? hud.GetComponent<Canvas>() : null;
        if (canvas == null) canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null) return false;

        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        var go = new GameObject("DamagePopups", typeof(RectTransform));
        go.transform.SetParent(canvas.transform, false);
        go.transform.SetAsFirstSibling();
        container = go.GetComponent<RectTransform>();
        container.anchorMin = Vector2.zero;
        container.anchorMax = Vector2.one;
        container.offsetMin = Vector2.zero;
        container.offsetMax = Vector2.zero;
        return true;
    }
}
