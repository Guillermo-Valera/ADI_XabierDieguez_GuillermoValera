using System.Collections.Generic;
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

    [SerializeField] private Animator animator;
    private string currentAnim;
    private float attackAnimTimer;
    private float attackClipLength = 0.5f;
    private float deathClipLength = 0.6f;

    private float maxHealth;
    private bool isDead;
    private UnitHealthBar healthBar;

    private UnitSfx sfx;

    [SerializeField] private bool isBase;

    public bool IsDead => isDead;
    public bool IsBase => isBase;
    public float Health => Mathf.Max(0, health);
    public float MaxHealth => maxHealth;
    public float HealthPercent => maxHealth > 0 ? Mathf.Clamp01(health / maxHealth) : 0f;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
        sfx = GetComponent<UnitSfx>();

        if (animator != null && animator.runtimeAnimatorController != null)
        {
            foreach (var clip in animator.runtimeAnimatorController.animationClips)
            {
                if (clip.name.EndsWith("_attack")) attackClipLength = clip.length;
                else if (clip.name.EndsWith("_death")) deathClipLength = clip.length;
            }
        }
    }

    private void Start()
    {
        timeRemaining = attackRate > 0 ? 1f / attackRate : 1f;
        maxHealth = health;

        if (isBase)
        {
            if (direction >= 0) ChangeLayer(6, 7);
            else ChangeLayer(7, 6);
            PlayAnim("idle");
        }

        healthBar = GetComponentInChildren<UnitHealthBar>();
        if (healthBar == null)
        {
            var go = new GameObject("HealthBar");
            go.transform.SetParent(transform, false);
            healthBar = go.AddComponent<UnitHealthBar>();
        }
        healthBar.SetTeam(direction >= 0);
        healthBar.SetValue(HealthPercent);
    }

    private void Update()
    {
        if (isDead || isBase) return;

        if (attackAnimTimer > 0)
            attackAnimTimer -= Time.deltaTime;

        if (!hasDetectedEnemy)
        {
            Move();
        }
        else
        {
            if (attackAnimTimer <= 0)
                PlayAnim("idle");
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
        PlayAnim("run");
        transform.Translate(Vector2.right * (direction * speed * Time.deltaTime));
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isDead || isBase) return;
        if (other.isTrigger) return;
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

            PlayAttackAnim();

            if (canAttackAtSeveralTargetsAtOnce)
            {
                VerifyTargets();
                foreach (var t in targets.ToArray())
                    Attack(t.GetComponent<Unit>());
            }
            else
                Attack(targets[0].GetComponent<Unit>());

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
        if (isDead) return;

        health -= damage;
        if (healthBar != null) healthBar.SetValue(HealthPercent);
        if (isBase && health > 0 && sfx != null) sfx.PlayHit();

        if (health <= 0)
            Die();
    }

    private void Die()
    {
        isDead = true;
        if (sfx != null) sfx.PlayDeath();

        foreach (var col in GetComponents<Collider2D>())
            col.enabled = false;

        if (healthBar != null) healthBar.gameObject.SetActive(false);

        if (isBase)
        {
            PlayAnim("death", true);
            GameUI.EndGame(leftWon: direction < 0);
            return;
        }

        if (animator != null)
        {
            PlayAnim("death", true);
            Destroy(gameObject, deathClipLength);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void VerifyTargets()
    {
        targets.RemoveAll(target => target == null ||
                                    (target.TryGetComponent(out Unit u) && u.IsDead));
    }

    protected void PlayImpactSfx()
    {
        if (sfx != null) sfx.PlayImpact();
    }

    private void PlayAttackAnim()
    {
        if (sfx != null) sfx.PlayAttack();
        if (animator == null) return;
        animator.Play("attack", 0, 0f);
        currentAnim = "attack";
        attackAnimTimer = attackClipLength;
    }

    private void PlayAnim(string state, bool restart = false)
    {
        if (animator == null) return;
        if (!restart && currentAnim == state) return;
        animator.Play(state, 0, 0f);
        currentAnim = state;
    }
}
