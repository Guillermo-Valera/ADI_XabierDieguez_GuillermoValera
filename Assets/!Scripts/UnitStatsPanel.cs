using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class UnitStatsPanel : MonoBehaviour
{
    private static readonly string[] Keys = { "Q / 1", "W / 2", "E / 3", "R / 4", "T / 5" };

    private static readonly (string title, float width)[] Columns =
    {
        ("Tecla", 80), ("Tropa", 120), ("Coste", 80), ("Vida", 80), ("Poder de ataque", 150),
        ("Atq/s", 80), ("DPS hoja", 110), ("DPS medido", 130), ("Velocidad", 110), ("Rango", 80)
    };

    private UnitSpawner spawner;
    private GameObject panel;
    private Text[] columnTexts;
    private float refreshTimer;

    public void Init(UnitSpawner unitSpawner)
    {
        spawner = unitSpawner;
        Build();
        Refresh();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab) && panel != null)
            panel.SetActive(!panel.activeSelf);

        if (Input.GetKeyDown(KeyCode.Backspace))
        {
            PlaytestStats.Reset();
            Refresh();
        }

        refreshTimer -= Time.unscaledDeltaTime;
        if (refreshTimer <= 0f)
        {
            refreshTimer = 0.25f;
            Refresh();
        }
    }

    private void Build()
    {
        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        float totalWidth = 40;
        foreach (var c in Columns) totalWidth += c.width;

        panel = new GameObject("UnitStatsPanel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(transform, false);
        var prt = panel.GetComponent<RectTransform>();
        prt.anchorMin = prt.anchorMax = new Vector2(0.5f, 0f);
        prt.pivot = new Vector2(0.5f, 0f);
        prt.anchoredPosition = new Vector2(0, 16);
        prt.sizeDelta = new Vector2(totalWidth, 250);
        var bg = panel.GetComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.72f);
        bg.raycastTarget = false;

        var title = CreateText("Title", panel.transform, font, 22, TextAnchor.UpperLeft);
        var trt = title.rectTransform;
        trt.anchorMin = new Vector2(0, 1);
        trt.anchorMax = new Vector2(1, 1);
        trt.pivot = new Vector2(0, 1);
        trt.anchoredPosition = new Vector2(20, -10);
        trt.sizeDelta = new Vector2(-40, 30);
        title.color = new Color(0.75f, 0.85f, 0.9f);
        title.text = "PARÁMETROS DE LAS UNIDADES  ·  Tab: ocultar  ·  Retroceso: reiniciar medición";

        columnTexts = new Text[Columns.Length];
        float x = 20;
        for (int i = 0; i < Columns.Length; i++)
        {
            var t = CreateText(Columns[i].title, panel.transform, font, 22, TextAnchor.UpperCenter);
            var rt = t.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(x, -46);
            rt.sizeDelta = new Vector2(Columns[i].width, 200);
            t.supportRichText = true;
            t.lineSpacing = 1.15f;
            columnTexts[i] = t;
            x += Columns[i].width;
        }
    }

    private static Text CreateText(string name, Transform parent, Font font, int size, TextAnchor anchor)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        var t = go.GetComponent<Text>();
        t.font = font;
        t.fontSize = size;
        t.alignment = anchor;
        t.color = Color.white;
        t.raycastTarget = false;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        return t;
    }

    private void Refresh()
    {
        if (spawner == null || columnTexts == null) return;

        var sb = new StringBuilder[Columns.Length];
        for (int i = 0; i < Columns.Length; i++)
            sb[i] = new StringBuilder("<b>" + Columns[i].title + "</b>");

        for (int i = 0; i < spawner.units.Length; i++)
        {
            var prefab = spawner.units[i];
            var unit = prefab != null ? prefab.GetComponent<Unit>() : null;
            if (unit == null) continue;

            float expected = unit.damage * unit.AttackRate;
            var stats = PlaytestStats.Get(unit.StatKey);
            string measured = "—";
            if (stats.MeasuredDps > 0f)
            {
                float diff = expected > 0 ? Mathf.Abs(stats.MeasuredDps - expected) / expected : 0f;
                string color = diff <= 0.1f ? "#7CFC9A" : "#FFB347";
                measured = $"<color={color}>{stats.MeasuredDps:0.#}</color>";
            }

            string[] values =
            {
                i < Keys.Length ? Keys[i] : "",
                unit.StatKey,
                unit.price.ToString(),
                unit.BaseHealth.ToString("0.##"),
                unit.damage.ToString("0.##"),
                unit.AttackRate.ToString("0.##"),
                expected.ToString("0.##"),
                measured,
                unit.Speed.ToString("0.##"),
                unit.Range.ToString("0.##")
            };

            for (int c = 0; c < Columns.Length; c++)
                sb[c].Append('\n').Append(values[c]);
        }

        for (int c = 0; c < Columns.Length; c++)
            columnTexts[c].text = sb[c].ToString();
    }
}
