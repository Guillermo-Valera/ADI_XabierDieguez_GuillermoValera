using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Unit : MonoBehaviour
{
    public int price;
    [SerializeField] private float health;
    [SerializeField] public float damage;
    [SerializeField] private float attackRate;
    [SerializeField] private bool canAttackAtSeveralTargetsAtOnce;
    [SerializeField] private bool killSelfAfterAttack;
    [SerializeField] private List<GameObject> targets;
    [SerializeField] private float speed;

    public LayerMask enemyLayer;
    public int direction;

    private bool hasDetectedEnemy;
    [SerializeField] float timeRemaining;
    
    private void Start()
    {
        timeRemaining = 1f / attackRate;
    }

    private void Update()
    {
        if (!hasDetectedEnemy)
        {
            Move();
        }
        else
        {
            AttackProcess();
        }

    }
    
    public void ChangeLayer(int ownLayer, int targetLayer)
    {
        gameObject.layer = ownLayer;
        enemyLayer = 1 << targetLayer;
    }


    private void Move()
    {
        transform.Translate(Vector2.right * (direction * speed * Time.deltaTime));
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.isTrigger) return; // ignora el rango de detección del otro
        if (((1 << other.gameObject.layer) & enemyLayer) == 0) return;
        if (targets.Contains(other.gameObject)) return;

        hasDetectedEnemy = true;
        targets.Add(other.gameObject);
    }
    private void OnTriggerExit2D(Collider2D other)
    {
        targets.Remove(other.gameObject);
    }


    private void AttackProcess()
    {
        if (timeRemaining > 0 && targets.Count > 0)
        {
            timeRemaining -= Time.deltaTime;
        }
        else
        {
            VerifyTargets();
            if (targets.Count == 0)
            {
                hasDetectedEnemy = false;
                timeRemaining = 1f / attackRate;
                return;
            }

            timeRemaining -= Time.deltaTime;
            if (timeRemaining > 0) return;

            if (canAttackAtSeveralTargetsAtOnce)
                foreach (var t in targets)
                {
                    VerifyTargets();
                    Attack(t.GetComponent<Unit>());  //t.GetComponent<Unit>().Damage(damage);
                }
            else
                Attack(targets[0].GetComponent<Unit>());      //targets[0].GetComponent<Unit>().Damage(damage);

            if (killSelfAfterAttack)
            {
                Destroy(gameObject);
            }
            timeRemaining = 1f / attackRate;
        }
    }

    public virtual void Attack(Unit target)
    {
        
    } 

    public void Damage(float damage)
    {
        health -= damage;
        if  (health <= 0)
            Destroy(gameObject);
    }
    
    public void VerifyTargets()
    {
        // Remove all null targets
        targets.RemoveAll(target => target == null);
    }

}
