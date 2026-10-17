using UnityEngine;


public class UnitHealthBar : MonoBehaviour
{
    [Tooltip("Sprite blanco con el pivote a la izquierda")]
    [SerializeField] private Sprite barSprite;
    [Tooltip("Material sin iluminación para que los colores se vean tal cual")]
    [SerializeField] private Material barMaterial;

    [SerializeField] private float width = 1.2f;
    [SerializeField] private float height = 0.15f;
    [SerializeField] private float yOffset = 1.5f;
    [SerializeField] private Color leftTeamColor = new Color(0.25f, 0.75f, 1f);
    [SerializeField] private Color rightTeamColor = new Color(1f, 0.35f, 0.3f);
    [SerializeField] private Color backgroundColor = new Color(0f, 0f, 0f, 0.7f);

    private Transform fill;
    private SpriteRenderer fillRenderer;

    private void Awake()
    {
        Build();
    }

    private void Build()
    {
        if (fill != null) return;

        
        if (barSprite == null)
        {
            var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            var pixels = new Color32[16];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color32(255, 255, 255, 255);
            tex.SetPixels32(pixels);
            tex.Apply();
            barSprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0f, 0.5f), 4);
        }

        transform.localPosition = new Vector3(0, yOffset, 0);

        var bg = CreatePart("Background", backgroundColor, 100);
        bg.transform.localPosition = new Vector3(-width / 2f - 0.03f, 0, 0);
        bg.transform.localScale = new Vector3(width + 0.06f, height + 0.06f, 1);

        fillRenderer = CreatePart("Fill", leftTeamColor, 101);
        fill = fillRenderer.transform;
        fill.localPosition = new Vector3(-width / 2f, 0, 0);
        fill.localScale = new Vector3(width, height, 1);
    }

    private SpriteRenderer CreatePart(string partName, Color color, int order)
    {
        var sr = new GameObject(partName).AddComponent<SpriteRenderer>();
        sr.transform.SetParent(transform, false);
        sr.sprite = barSprite;
        if (barMaterial != null) sr.sharedMaterial = barMaterial;
        sr.color = color;
        sr.sortingOrder = order;
        return sr;
    }

    public void SetTeam(bool leftTeam)
    {
        Build();
        fillRenderer.color = leftTeam ? leftTeamColor : rightTeamColor;
    }

    public void SetValue(float percent)
    {
        Build();
        fill.localScale = new Vector3(width * Mathf.Clamp01(percent), height, 1);
    }

    private void LateUpdate()
    {
       
        if (transform.parent == null) return;
        float sign = Mathf.Sign(transform.parent.lossyScale.x);
        transform.localScale = new Vector3(sign, 1, 1);
    }
}
