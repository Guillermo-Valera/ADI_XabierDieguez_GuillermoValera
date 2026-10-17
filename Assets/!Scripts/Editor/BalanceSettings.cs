#if UNITY_EDITOR
using UnityEngine;

public class BalanceSettings : ScriptableObject
{
    [TextArea(2, 4)]
    public string googleSheetUrl = "";

    public bool syncOnPlay = true;

    [Min(0)] public float autoSyncSeconds = 5f;
}
#endif
