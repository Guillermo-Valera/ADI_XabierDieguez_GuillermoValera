using Unity.VisualScripting;
using UnityEngine;

public class UnitSpawner : MonoBehaviour
{

    public GameObject[] units;

    [SerializeField] private Transform lSpawn;
    public int lMoney;
    
    [SerializeField] private Transform rSpawn;
    public int rMoney;

    private Unit go;
    private int typeOfUnit;
    void Update()
    {
        if ((Input.GetKeyDown(KeyCode.Q) && lMoney > units[0].GetComponent<Unit>().price) || (Input.GetKeyDown(KeyCode.Alpha1) && rMoney > units[0].GetComponent<Unit>().price))
        {
            typeOfUnit = 0;
        }
        
        if ((Input.GetKeyDown(KeyCode.W) && lMoney > units[1].GetComponent<Unit>().price) || (Input.GetKeyDown(KeyCode.Alpha2) && rMoney > units[1].GetComponent<Unit>().price))
        {
            typeOfUnit = 1;
        }
        
        if ((Input.GetKeyDown(KeyCode.E) && lMoney > units[2].GetComponent<Unit>().price) || (Input.GetKeyDown(KeyCode.Alpha3) && rMoney > units[2].GetComponent<Unit>().price))
        {
            typeOfUnit = 2;
        }
        
        if ((Input.GetKeyDown(KeyCode.R) && lMoney > units[3].GetComponent<Unit>().price) || (Input.GetKeyDown(KeyCode.Alpha4) && rMoney > units[3].GetComponent<Unit>().price))
        {
            typeOfUnit = 3;
        }
        
        if ((Input.GetKeyDown(KeyCode.T) && lMoney > units[4].GetComponent<Unit>().price) || (Input.GetKeyDown(KeyCode.Alpha5) && rMoney > units[4].GetComponent<Unit>().price))
        {
            typeOfUnit = 4;
        }
        
        
        if (Input.GetKeyDown(KeyCode.Q) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.T))
        {
            go = Instantiate(units[typeOfUnit], lSpawn.position, lSpawn.rotation).GetComponent<Unit>();
            lMoney -= units[typeOfUnit].GetComponent<Unit>().price;
            go.ChangeLayer(6, 7);
            go.direction = 1;
        }
        
        
        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Alpha5))
        {
            go = Instantiate(units[typeOfUnit], rSpawn.position, rSpawn.rotation).GetComponent<Unit>();
            rMoney -= units[typeOfUnit].GetComponent<Unit>().price;
            go.ChangeLayer(7, 6);
            go.direction = -1;
            go.gameObject.transform.localScale = new Vector3(-1, 1, 1);
        }
    }
}
