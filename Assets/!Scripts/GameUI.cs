using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// HUD de la partida: oro, vida de las bases, tiempo y el cartel de victoria/derrota.
/// Lo crea el menú Tools > Configurar personajes, proyectiles y UI.
/// </summary>
public class GameUI : MonoBehaviour
{
    [SerializeField] private UnitSpawner spawner;
    [SerializeField] private Text leftMoneyText;
    [SerializeField] private Text rightMoneyText;
    [SerializeField] private Text timerText;
    [SerializeField] private Text resultText;

    [Tooltip("Duración de la partida en segundos. 0 = el reloj cuenta hacia arriba.")]
    [SerializeField] private float matchDuration = 0f;

    [Tooltip("Segundos que se ve el cartel antes de reiniciar la escena.")]
    [SerializeField] private float restartDelay = 4f;

    public static bool IsGameOver { get; private set; }
    private static GameUI instance;

    private float elapsed;
    private Unit leftBase;
    private Unit rightBase;

    private void Awake()
    {
        instance = this;
        IsGameOver = false;
        Time.timeScale = 1f;
        if (resultText != null) ResultRoot.SetActive(false);
    }

    // El cartel puede estar dentro de un panel oscuro; se activa/desactiva el conjunto
    private GameObject ResultRoot =>
        resultText.transform.parent != null && resultText.transform.parent.GetComponent<Image>() != null
            ? resultText.transform.parent.gameObject
            : resultText.gameObject;

    private void Start()
    {
        if (spawner == null)
            spawner = FindFirstObjectByType<UnitSpawner>();

        foreach (var u in FindObjectsByType<Unit>(FindObjectsSortMode.None))
        {
            if (!u.IsBase) continue;
            if (u.direction >= 0) leftBase = u;
            else rightBase = u;
        }
    }

    private void Update()
    {
        if (!IsGameOver) elapsed += Time.deltaTime;

        if (spawner != null)
        {
            if (leftMoneyText != null)
                leftMoneyText.text = $"Jugador 1 (Q W E R T)\nBase: {BaseText(leftBase)}\nOro: {spawner.lMoney} / {spawner.maxMoney}";
            if (rightMoneyText != null)
                rightMoneyText.text = $"Jugador 2 (1 2 3 4 5)\nBase: {BaseText(rightBase)}\nOro: {spawner.rMoney} / {spawner.maxMoney}";
        }

        if (timerText != null)
        {
            float t = matchDuration > 0 ? Mathf.Max(0, matchDuration - elapsed) : elapsed;
            int minutes = (int)(t / 60f);
            int seconds = (int)(t % 60f);
            timerText.text = $"{minutes:00}:{seconds:00}";
        }
    }

    private static string BaseText(Unit b)
    {
        if (b == null) return "-";
        return $"{Mathf.CeilToInt(b.Health)} / {Mathf.CeilToInt(b.MaxHealth)}";
    }

    /// <summary>Lo llama la base al ser destruida.</summary>
    public static void EndGame(bool leftWon)
    {
        if (IsGameOver) return;
        IsGameOver = true;
        if (instance != null)
            instance.StartCoroutine(instance.ShowResultAndRestart(leftWon));
    }

    private IEnumerator ShowResultAndRestart(bool leftWon)
    {
        if (resultText != null)
        {
            ResultRoot.SetActive(true);
            resultText.text = leftWon ? "YOU WON" : "YOU LOSE";
            resultText.color = leftWon ? new Color(0.45f, 0.85f, 1f) : new Color(1f, 0.35f, 0.3f);
        }

        // Congela el combate mientras se ve el cartel
        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(restartDelay);

        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
