#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class GameModeSetup
{
    const string ScenePath = "Assets/Scenes/SampleScene.unity";
    const string UnitsAnimFolder = "Assets/Art/Duelyst-Sprites/Animations/Units/";
    const string BackgroundPath = "Assets/Art/Background/Battlefield.png";

    const float BaseHealth = 300f;
    const float BaseVisualHeight = 7f;
    const float BaseX = 12f;
    const float BaseY = 0f;

    static readonly (string prefab, string duelystUnit, int direction)[] Bases =
    {
        ("Assets/Prefab/Bases/LeftBase.prefab",  "f3_deathobelysk",     1),
        ("Assets/Prefab/Bases/RightBase.prefab", "f3_lavastormobelysk", -1),
    };

    static readonly (string prefab, int price)[] Prices =
    {
        ("Assets/Prefab/Melee/Brawler.prefab",        25),
        ("Assets/Prefab/Melee/Rogue.prefab",          40),
        ("Assets/Prefab/Ranged_Unique/Archer.prefab", 50),
        ("Assets/Prefab/Melee/Tank.prefab",           75),
        ("Assets/Prefab/Ranged_Area/Bomber.prefab",   90),
    };
    const int StartMoney = 50;
    const int MoneyPerTick = 5;
    const float TicksPerSecond = 1f;
    const int MaxMoney = 200;

    [MenuItem("Tools/Configurar bases, fondo y oro")]
    public static void SetupBasesBackgroundAndGold()
    {
        SetPrices();
        var basePrefabs = Bases.Select(b => CreateBasePrefab(b.prefab, b.duelystUnit, b.direction)).ToArray();
        SetupSfx();
        AssetDatabase.SaveAssets();

        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path != ScenePath)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            scene = EditorSceneManager.OpenScene(ScenePath);
        }

        PlaceBases(basePrefabs);
        SetupBackground();
        SetupSpawnerMoney();

        BalanceSync.Sync(saveScene: false);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    const string SfxFolder = "Assets/Audio/SFX/";

    static readonly (string prefab, string attack, string impact, string hit, string death)[] UnitSounds =
    {
        ("Assets/Prefab/Melee/Brawler.prefab",        "sfx_f2tank_attack_swing", "sfx_f2tank_attack_impact", "sfx_f2tank_hit_3", "sfx_f2tank_death"),
        ("Assets/Prefab/Melee/Rogue.prefab",          "sfx_f1_windbladecommander_attack_swing", "sfx_f1_windbladecommander_attack_impact", "sfx_f1_windbladecommander_hit", "sfx_f1_elyxstormblade_death"),
        ("Assets/Prefab/Melee/Tank.prefab",           "sfx_f1tank_attack_swing", "sfx_f1tank_attack_impact", "sfx_f1tank_hit", "sfx_f1tank_death"),
        ("Assets/Prefab/Ranged_Area/Bomber.prefab",   "sfx_neutral_firespitter_attack_swing", null, "sfx_neutral_firespitter_hit", "sfx_neutral_firespitter_death"),
        ("Assets/Prefab/Ranged_Unique/Archer.prefab", "sfx_f1_sunriser_attack_swing", null, "sfx_neutral_gambitgirl_hit", "sfx_f6_icedryad_death"),
        ("Assets/Prefab/Bases/LeftBase.prefab",       null, null, "sfx_neutral_spiritscribe_hit", "sfx_neutral_golembloodshard_death"),
        ("Assets/Prefab/Bases/RightBase.prefab",      null, null, "sfx_neutral_spiritscribe_hit", "sfx_neutral_golembloodshard_death"),
    };

    static readonly (string prefab, string impact)[] ProjectileSounds =
    {
        ("Assets/Prefab/Projectiles/Arrow.prefab", "sfx_neutral_swornavenger_attack_impact"),
        ("Assets/Prefab/Projectiles/Bomb.prefab",  "sfx_neutral_firespitter_attack_impact"),
    };

    static AudioClip Clip(string name) =>
        string.IsNullOrEmpty(name) ? null : AssetDatabase.LoadAssetAtPath<AudioClip>(SfxFolder + name + ".ogg");

    static void SetupSfx()
    {
        foreach (var (path, attack, impact, hit, death) in UnitSounds)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;
            var sfx = prefab.GetComponent<UnitSfx>();
            if (sfx == null) sfx = prefab.AddComponent<UnitSfx>();
            sfx.attack = Clip(attack);
            sfx.impact = Clip(impact);
            sfx.hit = Clip(hit);
            sfx.death = Clip(death);
            EditorUtility.SetDirty(prefab);
        }

        foreach (var (path, impact) in ProjectileSounds)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;
            var proj = prefab.GetComponent<ParabolicProjectile>();
            if (proj == null) continue;
            var so = new SerializedObject(proj);
            so.FindProperty("impactSfx").objectReferenceValue = Clip(impact);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(prefab);
        }
        AssetDatabase.SaveAssets();
    }

    static void SetPrices()
    {
        foreach (var (path, price) in Prices)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;
            var unit = prefab.GetComponent<Unit>();
            if (unit == null) continue;
            var so = new SerializedObject(unit);
            so.FindProperty("price").intValue = price;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(prefab);
        }
    }

    static void SetupSpawnerMoney()
    {
        var spawner = Object.FindFirstObjectByType<UnitSpawner>();
        if (spawner == null) return;
        var so = new SerializedObject(spawner);
        so.FindProperty("lMoney").intValue = StartMoney;
        so.FindProperty("rMoney").intValue = StartMoney;
        so.FindProperty("moneyGained").intValue = MoneyPerTick;
        so.FindProperty("moneyGainRate").floatValue = TicksPerSecond;
        so.FindProperty("maxMoney").intValue = MaxMoney;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static GameObject CreateBasePrefab(string prefabPath, string duelystUnit, int direction)
    {
        var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
            $"{UnitsAnimFolder}{duelystUnit}/{duelystUnit}.controller");
        if (controller == null)
        {
            Debug.LogError($"[GameModeSetup] No encuentro el controller de {duelystUnit}");
            return null;
        }
        var idle = controller.animationClips.FirstOrDefault(c => c.name.EndsWith("_idle"));
        var sprite = GameSetupTool.FirstSprite(idle);

        string folder = System.IO.Path.GetDirectoryName(prefabPath).Replace('\\', '/');
        if (!AssetDatabase.IsValidFolder(folder))
            AssetDatabase.CreateFolder("Assets/Prefab", "Bases");

        bool exists = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null;
        var root = exists
            ? PrefabUtility.LoadPrefabContents(prefabPath)
            : new GameObject(System.IO.Path.GetFileNameWithoutExtension(prefabPath));

        try
        {
            var visual = GameSetupTool.GetOrCreateChild(root.transform, "Visual");
            var sr = GameSetupTool.GetOrAdd<SpriteRenderer>(visual.gameObject);
            sr.sprite = sprite;
            sr.color = Color.white;
            sr.sortingOrder = 5;

            var unlitMat = GameSetupTool.GetUnlitMaterial();
            if (unlitMat != null) sr.sharedMaterial = unlitMat;
            var anim = GameSetupTool.GetOrAdd<Animator>(visual.gameObject);
            anim.runtimeAnimatorController = controller;
            GameSetupTool.FitHeight(visual, sprite, BaseVisualHeight);

            var col = GameSetupTool.GetOrAdd<CircleCollider2D>(root);
            col.isTrigger = false;
            col.radius = 1.2f;
            var rb = GameSetupTool.GetOrAdd<Rigidbody2D>(root);
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0;

            var unit = GameSetupTool.GetOrAdd<Unit>(root);
            var so = new SerializedObject(unit);
            so.FindProperty("price").intValue = 0;
            so.FindProperty("health").floatValue = BaseHealth;
            so.FindProperty("damage").floatValue = 0;
            so.FindProperty("attackRate").floatValue = 1;
            so.FindProperty("speed").floatValue = 0;
            so.FindProperty("direction").intValue = direction;
            so.FindProperty("isBase").boolValue = true;
            so.FindProperty("animator").objectReferenceValue = anim;
            so.ApplyModifiedPropertiesWithoutUndo();

            var barT = GameSetupTool.GetOrCreateChild(root.transform, "HealthBar");
            foreach (Transform child in barT.Cast<Transform>().ToList())
                Object.DestroyImmediate(child.gameObject);
            var bar = GameSetupTool.GetOrAdd<UnitHealthBar>(barT.gameObject);
            var barSo = new SerializedObject(bar);
            barSo.FindProperty("barSprite").objectReferenceValue = GameSetupTool.GetBarSprite();
            barSo.FindProperty("barMaterial").objectReferenceValue = GameSetupTool.GetUnlitMaterial();
            barSo.FindProperty("width").floatValue = 3f;
            barSo.FindProperty("height").floatValue = 0.3f;
            barSo.FindProperty("yOffset").floatValue = 2.8f;
            barSo.ApplyModifiedPropertiesWithoutUndo();

            return PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        }
        finally
        {
            if (exists) PrefabUtility.UnloadPrefabContents(root);
            else Object.DestroyImmediate(root);
        }
    }

    static void PlaceBases(GameObject[] prefabs)
    {
        foreach (var u in Object.FindObjectsByType<Unit>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (u.IsBase) Object.DestroyImmediate(u.gameObject);

        for (int i = 0; i < prefabs.Length; i++)
        {
            if (prefabs[i] == null) continue;
            int dir = Bases[i].direction;
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefabs[i]);
            go.transform.position = new Vector3(-BaseX * dir, BaseY, 0);

            go.transform.localScale = new Vector3(dir, 1, 1);
        }
    }

    static void SetupBackground()
    {
        var importer = AssetImporter.GetAtPath(BackgroundPath) as TextureImporter;
        if (importer == null)
        {
            Debug.LogWarning($"[GameModeSetup] No encuentro {BackgroundPath}");
            return;
        }
        if (importer.textureType != TextureImporterType.Sprite)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(BackgroundPath);

        var old = GameObject.Find("Background");
        if (old != null) Object.DestroyImmediate(old);

        var bg = new GameObject("Background");
        var sr = bg.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = -100;
        var unlit = GameSetupTool.GetUnlitMaterial();
        if (unlit != null) sr.sharedMaterial = unlit;

        var cam = Camera.main;
        float camH = cam != null ? cam.orthographicSize * 2f : 16f;
        float camW = camH * 16f / 9f;
        var size = sprite.bounds.size;
        float s = Mathf.Max(camW / size.x, camH / size.y) * 1.05f;
        bg.transform.position = new Vector3(cam != null ? cam.transform.position.x : 0, cam != null ? cam.transform.position.y : 0, 0);
        bg.transform.localScale = new Vector3(s, s, 1);
    }
}
#endif
