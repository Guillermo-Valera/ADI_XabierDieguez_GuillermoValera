#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Configura de una vez los personajes, proyectiles y el HUD.
/// Menú: Tools > Configurar personajes, proyectiles y UI
/// Se puede ejecutar varias veces: reutiliza lo que ya exista.
/// </summary>
public static class GameSetupTool
{
    const string UnitsAnimFolder = "Assets/Art/Duelyst-Sprites/Animations/Units/";
    const string ProjectileControllersFolder = "Assets/Art/Projectiles";

    // Altura (en unidades de mundo) que ocupará el frame completo del sprite.
    const float UnitVisualHeight = 5f;
    const float ProjectileVisualHeight = 3.5f;

    // Prefab -> personaje de Duelyst
    static readonly (string prefab, string duelystUnit)[] UnitMap =
    {
        ("Assets/Prefab/Melee/Brawler.prefab",         "f2_tank"),
        ("Assets/Prefab/Melee/Rogue.prefab",           "f4_shadowdancer"),
        ("Assets/Prefab/Melee/Tank.prefab",            "f1_shieldforger"),
        ("Assets/Prefab/Ranged_Area/Bomber.prefab",    "neutral_mercgrenadier"),
        ("Assets/Prefab/Ranged_Unique/Archer.prefab",  "f1_backlinearcher"),
    };

    // Proyectil -> clip de proyectil de Duelyst
    static readonly (string prefab, string duelystUnit)[] ProjectileMap =
    {
        ("Assets/Prefab/Projectiles/Arrow.prefab", "f1_backlinearcher"),
        ("Assets/Prefab/Projectiles/Bomb.prefab",  "neutral_mercgrenadier"),
    };

    [MenuItem("Tools/Configurar personajes, proyectiles y UI")]
    public static void SetupAll()
    {
        int ok = 0;
        foreach (var (prefab, unit) in UnitMap)
            if (SetupUnitPrefab(prefab, unit)) ok++;

        foreach (var (prefab, unit) in ProjectileMap)
            if (SetupProjectilePrefab(prefab, unit)) ok++;

        AssetDatabase.SaveAssets();

        SetupHud();
        GameModeSetup.SetupBasesBackgroundAndGold();

        Debug.Log($"[GameSetupTool] Listo: {ok}/{UnitMap.Length + ProjectileMap.Length} prefabs configurados y HUD creado.");
    }

    // ---------------- Personajes ----------------

    static bool SetupUnitPrefab(string prefabPath, string duelystUnit)
    {
        string controllerPath = $"{UnitsAnimFolder}{duelystUnit}/{duelystUnit}.controller";
        var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(controllerPath);
        if (controller == null)
        {
            Debug.LogError($"[GameSetupTool] No encuentro {controllerPath}");
            return false;
        }

        var idleClip = controller.animationClips.FirstOrDefault(c => c.name.EndsWith("_idle"))
                       ?? controller.animationClips.FirstOrDefault();
        Sprite firstSprite = FirstSprite(idleClip);

        var root = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            // El sprite pasa a un hijo "Visual" para poder escalarlo sin tocar los colliders
            var oldRenderer = root.GetComponent<SpriteRenderer>();
            if (oldRenderer != null) Object.DestroyImmediate(oldRenderer);
            var oldAnimator = root.GetComponent<Animator>();
            if (oldAnimator != null) Object.DestroyImmediate(oldAnimator);

            var visual = GetOrCreateChild(root.transform, "Visual");
            var sr = GetOrAdd<SpriteRenderer>(visual.gameObject);
            sr.sprite = firstSprite;
            sr.color = Color.white;
            sr.sortingOrder = 10;

            var anim = GetOrAdd<Animator>(visual.gameObject);
            anim.runtimeAnimatorController = controller;
            anim.applyRootMotion = false;

            FitHeight(visual, firstSprite, UnitVisualHeight);

            // Barra de vida con sprite y material sin iluminación
            var barT = GetOrCreateChild(root.transform, "HealthBar");
            foreach (Transform child in barT.Cast<Transform>().ToList())
                Object.DestroyImmediate(child.gameObject);
            var bar = GetOrAdd<UnitHealthBar>(barT.gameObject);
            var barSo = new SerializedObject(bar);
            barSo.FindProperty("barSprite").objectReferenceValue = GetBarSprite();
            barSo.FindProperty("barMaterial").objectReferenceValue = GetUnlitMaterial();
            barSo.ApplyModifiedPropertiesWithoutUndo();

            // Referencia del Animator en Unit
            var unit = root.GetComponent<Unit>();
            if (unit != null)
            {
                var so = new SerializedObject(unit);
                var prop = so.FindProperty("animator");
                if (prop != null)
                {
                    prop.objectReferenceValue = anim;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Debug.Log($"[GameSetupTool] {prefabPath} -> {duelystUnit}");
            return true;
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    // ---------------- Proyectiles ----------------

    static bool SetupProjectilePrefab(string prefabPath, string duelystUnit)
    {
        string clipPath = $"{UnitsAnimFolder}{duelystUnit}/{duelystUnit}_projectile.anim";
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
        if (clip == null)
        {
            Debug.LogError($"[GameSetupTool] No encuentro {clipPath}");
            return false;
        }

        if (!AssetDatabase.IsValidFolder(ProjectileControllersFolder))
            AssetDatabase.CreateFolder("Assets/Art", "Projectiles");

        string name = System.IO.Path.GetFileNameWithoutExtension(prefabPath);
        string controllerPath = $"{ProjectileControllersFolder}/{name}.controller";
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (controller == null)
            controller = AnimatorController.CreateAnimatorControllerAtPathWithClip(controllerPath, clip);

        Sprite firstSprite = FirstSprite(clip);

        var root = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            var oldRenderer = root.GetComponent<SpriteRenderer>();
            if (oldRenderer != null) Object.DestroyImmediate(oldRenderer);

            var visual = GetOrCreateChild(root.transform, "Visual");
            var sr = GetOrAdd<SpriteRenderer>(visual.gameObject);
            sr.sprite = firstSprite;
            sr.color = Color.white;
            sr.sortingOrder = 20;

            var anim = GetOrAdd<Animator>(visual.gameObject);
            anim.runtimeAnimatorController = controller;

            FitHeight(visual, firstSprite, ProjectileVisualHeight);

            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Debug.Log($"[GameSetupTool] {prefabPath} -> {duelystUnit}_projectile");
            return true;
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    // ---------------- HUD ----------------

    static void SetupHud()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path != "Assets/Scenes/SampleScene.unity")
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            scene = EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        }

        var existing = GameObject.Find("HUD");
        if (existing != null) Object.DestroyImmediate(existing);

        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        var hud = new GameObject("HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = hud.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = hud.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        var left = CreateText(hud.transform, "LeftMoney", font, TextAnchor.UpperLeft,
            new Vector2(0, 1), new Vector2(30, -25), new Color(0.45f, 0.85f, 1f));
        var right = CreateText(hud.transform, "RightMoney", font, TextAnchor.UpperRight,
            new Vector2(1, 1), new Vector2(-30, -25), new Color(1f, 0.55f, 0.5f));
        var timer = CreateText(hud.transform, "Timer", font, TextAnchor.UpperCenter,
            new Vector2(0.5f, 1), new Vector2(0, -20), Color.white);
        timer.fontSize = 56;
        timer.text = "00:00";

        var result = CreateText(hud.transform, "Result", font, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), Vector2.zero, Color.white);
        result.fontSize = 140;
        result.text = "YOU WON";
        result.GetComponent<Outline>().effectDistance = new Vector2(5, -5);

        var ui = hud.AddComponent<GameUI>();
        var so = new SerializedObject(ui);
        so.FindProperty("spawner").objectReferenceValue = Object.FindFirstObjectByType<UnitSpawner>();
        so.FindProperty("leftMoneyText").objectReferenceValue = left;
        so.FindProperty("rightMoneyText").objectReferenceValue = right;
        so.FindProperty("timerText").objectReferenceValue = timer;
        so.FindProperty("resultText").objectReferenceValue = result;
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    static Text CreateText(Transform parent, string name, Font font, TextAnchor anchor,
                           Vector2 pivotAndAnchor, Vector2 position, Color color)
    {
        // Panel oscuro detrás para que el texto se lea sobre el fondo
        var panel = new GameObject(name + "Panel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(parent, false);
        var prt = panel.GetComponent<RectTransform>();
        prt.anchorMin = prt.anchorMax = prt.pivot = pivotAndAnchor;
        prt.anchoredPosition = position;
        panel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.55f);
        panel.GetComponent<Image>().raycastTarget = false;
        var fitter = panel.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        var layout = panel.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(20, 20, 10, 10);

        var go = new GameObject(name, typeof(RectTransform), typeof(Text), typeof(Outline));
        go.transform.SetParent(panel.transform, false);

        var text = go.GetComponent<Text>();
        text.font = font;
        text.fontSize = 34;
        text.fontStyle = FontStyle.Bold;
        text.alignment = anchor;
        text.color = color;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        text.text = name;

        var outline = go.GetComponent<Outline>();
        outline.effectColor = new Color(0, 0, 0, 0.85f);
        outline.effectDistance = new Vector2(2, -2);
        return text;
    }

    // ---------------- Barra de vida ----------------

    const string BarSpritePath = "Assets/Art/UI/HealthBarPixel.png";

    internal static Sprite GetBarSprite()
    {
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(BarSpritePath);
        if (sprite != null) return sprite;

        if (!AssetDatabase.IsValidFolder("Assets/Art/UI"))
            AssetDatabase.CreateFolder("Assets/Art", "UI");

        var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        var pixels = new Color32[16];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color32(255, 255, 255, 255);
        tex.SetPixels32(pixels);
        tex.Apply();
        System.IO.File.WriteAllBytes(BarSpritePath, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(BarSpritePath);

        var importer = (TextureImporter)AssetImporter.GetAtPath(BarSpritePath);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 4;
        importer.filterMode = FilterMode.Point;
        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteAlignment = (int)SpriteAlignment.Custom;
        settings.spritePivot = new Vector2(0f, 0.5f);
        importer.SetTextureSettings(settings);
        importer.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Sprite>(BarSpritePath);
    }

    internal static Material GetUnlitMaterial()
    {
        // Material sin luz del 2D Renderer de URP
        var mat = AssetDatabase.LoadAssetAtPath<Material>(
            "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat");
        if (mat != null) return mat;

        var guid = AssetDatabase.FindAssets("Sprite-Unlit-Default t:Material").FirstOrDefault();
        return guid != null ? AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid)) : null;
    }

    // ---------------- Utilidades ----------------

    internal static Sprite FirstSprite(AnimationClip clip)
    {
        if (clip == null) return null;
        foreach (var binding in AnimationUtility.GetObjectReferenceCurveBindings(clip))
        {
            var keys = AnimationUtility.GetObjectReferenceCurve(clip, binding);
            if (keys != null && keys.Length > 0 && keys[0].value is Sprite s)
                return s;
        }
        return null;
    }

    internal static void FitHeight(Transform visual, Sprite sprite, float targetHeight)
    {
        visual.localPosition = Vector3.zero;
        visual.localRotation = Quaternion.identity;
        if (sprite == null) { visual.localScale = Vector3.one; return; }
        float h = sprite.bounds.size.y;
        float s = h > 0 ? targetHeight / h : 1f;
        visual.localScale = new Vector3(s, s, 1);
    }

    internal static Transform GetOrCreateChild(Transform parent, string name)
    {
        var t = parent.Find(name);
        if (t != null) return t;
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go.transform;
    }

    internal static T GetOrAdd<T>(GameObject go) where T : Component
    {
        var c = go.GetComponent<T>();
        return c != null ? c : go.AddComponent<T>();
    }
}
#endif
