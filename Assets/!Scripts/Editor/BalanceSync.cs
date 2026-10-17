#if UNITY_EDITOR
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Networking;

[InitializeOnLoad]
public static class BalanceSync
{
    const string Folder = "Assets/Balance";
    const string SettingsPath = Folder + "/BalanceSettings.asset";
    const string LocalCsvPath = Folder + "/Balance.csv";

    static readonly Dictionary<string, string> TroopPrefabs = new Dictionary<string, string>
    {
        { "brawler", "Assets/Prefab/Melee/Brawler.prefab" },
        { "rogue",   "Assets/Prefab/Melee/Rogue.prefab" },
        { "tank",    "Assets/Prefab/Melee/Tank.prefab" },
        { "archer",  "Assets/Prefab/Ranged_Unique/Archer.prefab" },
        { "bomber",  "Assets/Prefab/Ranged_Area/Bomber.prefab" },
    };
    static readonly string[] BasePrefabs =
    {
        "Assets/Prefab/Bases/LeftBase.prefab",
        "Assets/Prefab/Bases/RightBase.prefab",
    };

    static readonly (string header, string field, bool isInt)[] TroopColumns =
    {
        ("coste",               "price",      true),
        ("vida",                "health",     false),
        ("dano",                "damage",     false),
        ("ataques por segundo", "attackRate", false),
        ("velocidad",           "speed",      false),
    };

    static BalanceSync()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
        EditorApplication.update += AutoSyncUpdate;
    }

    static double nextPoll;
    static UnityWebRequest pending;
    static string lastCsv;

    static void AutoSyncUpdate()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
            return;

        if (pending != null)
        {
            if (!pending.isDone) return;
            if (pending.result == UnityWebRequest.Result.Success)
            {
                string text = pending.downloadHandler.text;
                if (!text.TrimStart().StartsWith("<") && text != lastCsv)
                {
                    lastCsv = text;
                    var changes = Apply(ParseCsv(text), saveScene: false);
                    if (changes.Count > 0)
                    {
                        File.WriteAllText(LocalCsvPath, text, new UTF8Encoding(false));
                        AssetDatabase.ImportAsset(LocalCsvPath);
                        UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
                    }
                }
            }
            pending.Dispose();
            pending = null;
            return;
        }

        if (EditorApplication.timeSinceStartup < nextPoll) return;

        var settings = AssetDatabase.LoadAssetAtPath<BalanceSettings>(SettingsPath);
        if (settings == null || settings.autoSyncSeconds <= 0 || string.IsNullOrWhiteSpace(settings.googleSheetUrl))
        {
            nextPoll = EditorApplication.timeSinceStartup + 5;
            return;
        }
        nextPoll = EditorApplication.timeSinceStartup + Mathf.Max(2f, settings.autoSyncSeconds);

        string url = ToCsvUrl(settings.googleSheetUrl);
        if (url == null) return;
        pending = UnityWebRequest.Get(url);
        pending.timeout = 15;
        pending.SendWebRequest();
    }

    static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.ExitingEditMode) return;
        var settings = GetOrCreateSettings();
        if (settings.syncOnPlay) Sync(saveScene: false);
    }

    [MenuItem("Tools/Balance/Sincronizar desde la hoja")]
    static void SyncFromMenu() => Sync(saveScene: true);

    [MenuItem("Tools/Balance/Ajustes de la hoja")]
    static void SelectSettings()
    {
        Selection.activeObject = GetOrCreateSettings();
        EditorGUIUtility.PingObject(Selection.activeObject);
    }

    public static bool Sync(bool saveScene)
    {
        var settings = GetOrCreateSettings();
        string csv = null;
        string source = null;

        if (!string.IsNullOrWhiteSpace(settings.googleSheetUrl))
        {
            csv = Download(settings.googleSheetUrl, out string error);
            if (csv != null)
            {
                source = "Google Sheets";

                File.WriteAllText(LocalCsvPath, csv, new UTF8Encoding(false));
                AssetDatabase.ImportAsset(LocalCsvPath);
            }
            else
            {
                Debug.LogWarning($"[Balance] No se pudo leer la hoja de Google: {error}. Uso la copia local.");
            }
        }

        if (csv == null && File.Exists(LocalCsvPath))
        {
            csv = File.ReadAllText(LocalCsvPath);
            source = LocalCsvPath;
        }

        if (csv == null)
        {
            Debug.LogError($"[Balance] No hay hoja configurada ni {LocalCsvPath}.");
            return false;
        }

        if (source == "Google Sheets") lastCsv = csv;
        Apply(ParseCsv(csv), saveScene);

        return true;
    }

    static List<string> Apply(List<List<string>> rows, bool saveScene)
    {
        var changes = new List<string>();

        int headerRow = rows.FindIndex(r => r.Count > 0 && Normalize(r[0]) == "tropa");
        if (headerRow < 0)
        {
            Debug.LogError("[Balance] No encuentro la fila de títulos (la que empieza por 'Tropa').");
            return changes;
        }

        var colIndex = new Dictionary<string, int>();
        for (int c = 0; c < rows[headerRow].Count; c++)
            colIndex[Normalize(rows[headerRow][c])] = c;

        float? startMoney = null, moneyPerSecond = null, maxMoney = null;

        for (int r = headerRow + 1; r < rows.Count; r++)
        {
            var row = rows[r];
            if (row.Count == 0) continue;
            string key = Normalize(row[0]);

            if (TroopPrefabs.TryGetValue(key, out string prefabPath))
            {
                foreach (var (header, field, isInt) in TroopColumns)
                    if (colIndex.TryGetValue(header, out int c) && TryNumber(row, c, out float v))
                        SetUnitField(prefabPath, field, v, isInt, row[0], changes);
            }
            else if (key == "base")
            {
                if (colIndex.TryGetValue("vida", out int c) && TryNumber(row, c, out float v))
                    foreach (var basePath in BasePrefabs)
                        SetUnitField(basePath, "health", v, false, Path.GetFileNameWithoutExtension(basePath), changes);
            }
            else if (key == "oro inicial" && TryNumber(row, 1, out float a)) startMoney = a;
            else if (key == "oro por segundo" && TryNumber(row, 1, out float b)) moneyPerSecond = b;
            else if ((key == "oro maximo") && TryNumber(row, 1, out float m)) maxMoney = m;
        }

        AssetDatabase.SaveAssets();
        ApplyEconomy(startMoney, moneyPerSecond, maxMoney, saveScene, changes);
        return changes;
    }

    static void SetUnitField(string prefabPath, string field, float value, bool isInt, string label, List<string> changes)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        var unit = prefab != null ? prefab.GetComponent<Unit>() : null;
        if (unit == null) return;

        var so = new SerializedObject(unit);
        var prop = so.FindProperty(field);
        if (prop == null) return;

        if (isInt)
        {
            int newValue = Mathf.RoundToInt(value);
            if (prop.intValue == newValue) return;
            changes.Add($"{label} {field}: {prop.intValue} -> {newValue}");
            prop.intValue = newValue;
        }
        else
        {
            if (Mathf.Approximately(prop.floatValue, value)) return;
            changes.Add($"{label} {field}: {prop.floatValue} -> {value}");
            prop.floatValue = value;
        }
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(prefab);
    }

    static void ApplyEconomy(float? startMoney, float? moneyPerSecond, float? maxMoney, bool saveScene, List<string> changes)
    {
        if (startMoney == null && moneyPerSecond == null && maxMoney == null) return;

        var spawner = Object.FindFirstObjectByType<UnitSpawner>();
        if (spawner == null)
        {
            Debug.LogWarning("[Balance] Abre la escena del juego para aplicar el oro (no encuentro el UnitSpawner).");
            return;
        }

        var so = new SerializedObject(spawner);
        bool changed = false;

        void SetInt(string field, int v)
        {
            var p = so.FindProperty(field);
            if (p.intValue == v) return;
            changes.Add($"UnitSpawner {field}: {p.intValue} -> {v}");
            p.intValue = v; changed = true;
        }
        void SetFloat(string field, float v)
        {
            var p = so.FindProperty(field);
            if (Mathf.Approximately(p.floatValue, v)) return;
            changes.Add($"UnitSpawner {field}: {p.floatValue} -> {v}");
            p.floatValue = v; changed = true;
        }

        if (startMoney != null)
        {
            SetInt("lMoney", Mathf.RoundToInt(startMoney.Value));
            SetInt("rMoney", Mathf.RoundToInt(startMoney.Value));
        }
        if (maxMoney != null) SetInt("maxMoney", Mathf.RoundToInt(maxMoney.Value));
        if (moneyPerSecond != null)
        {
            float ps = moneyPerSecond.Value;
            if (Mathf.Approximately(ps, Mathf.Round(ps)))
            {
                SetInt("moneyGained", Mathf.RoundToInt(ps));
                SetFloat("moneyGainRate", 1f);
            }
            else
            {
                SetInt("moneyGained", 1);
                SetFloat("moneyGainRate", ps);
            }
        }

        if (!changed) return;
        so.ApplyModifiedPropertiesWithoutUndo();
        var scene = spawner.gameObject.scene;
        EditorSceneManager.MarkSceneDirty(scene);
        if (saveScene) EditorSceneManager.SaveScene(scene);
    }

    static string Download(string url, out string error)
    {
        error = null;
        string exportUrl = ToCsvUrl(url);
        if (exportUrl == null)
        {
            error = "el enlace no parece de Google Sheets";
            return null;
        }

        using (var req = UnityWebRequest.Get(exportUrl))
        {
            req.timeout = 15;
            var op = req.SendWebRequest();
            var start = EditorApplication.timeSinceStartup;
            while (!op.isDone)
            {
                if (EditorApplication.timeSinceStartup - start > 20) { error = "tiempo de espera agotado"; return null; }
                System.Threading.Thread.Sleep(20);
            }

            if (req.result != UnityWebRequest.Result.Success)
            {
                error = req.error;
                return null;
            }

            string text = req.downloadHandler.text;
            if (text.TrimStart().StartsWith("<"))
            {
                error = "Google ha devuelto una página web en vez de la hoja. Compártela como 'Cualquier persona con el enlace: Lector'";
                return null;
            }
            return text;
        }
    }

    static string ToCsvUrl(string url)
    {
        url = url.Trim();
        if (url.Contains("output=csv") || url.Contains("format=csv")) return url;

        var id = Regex.Match(url, @"/spreadsheets/d/([a-zA-Z0-9-_]+)");
        if (!id.Success) return null;
        var gid = Regex.Match(url, @"[#&?]gid=(\d+)");
        string g = gid.Success ? gid.Groups[1].Value : "0";
        return $"https://docs.google.com/spreadsheets/d/{id.Groups[1].Value}/export?format=csv&gid={g}";
    }

    static List<List<string>> ParseCsv(string text)
    {
        var rows = new List<List<string>>();
        var row = new List<string>();
        var field = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < text.Length; i++)
        {
            char ch = text[i];
            if (inQuotes)
            {
                if (ch == '"')
                {
                    if (i + 1 < text.Length && text[i + 1] == '"') { field.Append('"'); i++; }
                    else inQuotes = false;
                }
                else field.Append(ch);
            }
            else if (ch == '"') inQuotes = true;
            else if (ch == ',') { row.Add(field.ToString()); field.Clear(); }
            else if (ch == '\n' || ch == '\r')
            {
                if (ch == '\r' && i + 1 < text.Length && text[i + 1] == '\n') i++;
                row.Add(field.ToString()); field.Clear();
                rows.Add(row); row = new List<string>();
            }
            else field.Append(ch);
        }
        if (field.Length > 0 || row.Count > 0) { row.Add(field.ToString()); rows.Add(row); }
        return rows;
    }

    static bool TryNumber(List<string> row, int col, out float value)
    {
        value = 0;
        if (col < 0 || col >= row.Count) return false;
        string s = row[col].Trim().Replace(',', '.');
        return s.Length > 0 && float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }

    static string Normalize(string s)
    {
        if (s == null) return "";
        var formD = s.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (char ch in formD)
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark) sb.Append(ch);
        return sb.ToString().Normalize(NormalizationForm.FormC);
    }

    static BalanceSettings GetOrCreateSettings()
    {
        var settings = AssetDatabase.LoadAssetAtPath<BalanceSettings>(SettingsPath);
        if (settings != null) return settings;

        if (!AssetDatabase.IsValidFolder(Folder))
            AssetDatabase.CreateFolder("Assets", "Balance");
        settings = ScriptableObject.CreateInstance<BalanceSettings>();
        AssetDatabase.CreateAsset(settings, SettingsPath);
        AssetDatabase.SaveAssets();
        return settings;
    }
}
#endif
