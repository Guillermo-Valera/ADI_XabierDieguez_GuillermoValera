using UnityEngine;

public class UnitSpawner : MonoBehaviour
{

    public GameObject[] units;

    [SerializeField] private Transform lSpawn;
    public int lMoney;

    [SerializeField] private Transform rSpawn;
    public int rMoney;

    private float timeRemaining;
    public float moneyGainRate;
    public int moneyGained;
    public int maxMoney;

    private static readonly KeyCode[] leftKeys  = { KeyCode.Q, KeyCode.W, KeyCode.E, KeyCode.R, KeyCode.T };
    private static readonly KeyCode[] rightKeys = { KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4, KeyCode.Alpha5 };

    void Update()
    {
        if (GameUI.IsGameOver) return;

        GainMoneyProcess();

        for (int i = 0; i < units.Length && i < leftKeys.Length; i++)
        {
            if (Input.GetKeyDown(leftKeys[i]))
                TrySpawn(i, true);

            if (Input.GetKeyDown(rightKeys[i]))
                TrySpawn(i, false);
        }
    }

    // Solo saca la unidad si el jugador tiene oro suficiente
    private void TrySpawn(int typeOfUnit, bool left)
    {
        int price = units[typeOfUnit].GetComponent<Unit>().price;

        if (left)
        {
            if (lMoney < price) return;
            Unit go = Instantiate(units[typeOfUnit], lSpawn.position, lSpawn.rotation).GetComponent<Unit>();
            lMoney -= price;
            go.ChangeLayer(6, 7);
            go.direction = 1;
        }
        else
        {
            if (rMoney < price) return;
            Unit go = Instantiate(units[typeOfUnit], rSpawn.position, rSpawn.rotation).GetComponent<Unit>();
            rMoney -= price;
            go.ChangeLayer(7, 6);
            go.direction = -1;
            go.gameObject.transform.localScale = new Vector3(-1, 1, 1);
        }
    }

    private void GainMoneyProcess()
    {
        if (timeRemaining > 0)
        {
            timeRemaining -= Time.deltaTime;
        }
        else
        {
            rMoney += moneyGained;
            lMoney += moneyGained;

            if (lMoney > maxMoney)
            {
                lMoney = maxMoney;
            }

            if (rMoney > maxMoney)
            {
                rMoney = maxMoney;
            }
            timeRemaining = 1f / moneyGainRate;
        }
    }
}
