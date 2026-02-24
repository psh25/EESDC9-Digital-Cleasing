using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class Player : Entity
{
    [Header("Move Settings")]
    [SerializeField] public float actCooldown = 0.2f;
    [SerializeField] private float baseBpm = 120f;

    [SerializeField] private GameObject healthPrefab;
    public int health = 3;
    private readonly List<GameObject> activeHealthVisuals = new List<GameObject>();

    [SerializeField] private GameObject deathEffectPrefab;
    [SerializeField] private float deathEffectDuration = 0.2f;

    private float nextMoveTime;

    private Animator animator;

    //有限状态机
    public enum PlayerState: int 
    {
         Idle, //0
         Moving, //1
         Attacking, //2
         Dead //3
    }
    private PlayerState currentState = PlayerState.Idle;

    // 获取组件
    public override void Awake()
    {
        base.Awake();
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
        DisplayHealth();
    }

    //订阅OnBeat事件
    private void OnEnable()
    {
        BeatManager.OnBeat += OnBeat;
    }
    //消失时取消订阅
    private void OnDisable()
    {
        BeatManager.OnBeat -= OnBeat;
    }
    //同步到Animator
    private void OnBeat()
    {
        if (BeatManager.BeatIndex % 2 == 0)
        {
            animator.SetTrigger("OnEvenBeat");
        }
        SyncAnimatorSpeed();
    }
    //根据节拍调整动画速度
    private void SyncAnimatorSpeed()
    {
    var beatManager = FindObjectOfType<BeatManager>();
    if (beatManager == null) return;

    float ratio = beatManager.bpm / baseBpm;
    animator.speed = ratio;
    }

    // 运行期间每帧调用
    private void Update()
    {
        if (GridManager == null)
        {
            return;
        }

        // 根据当前状态设置动画参数
        animator.SetInteger("playerState", (int)currentState);
        
        //冷却中无法行动
        if (Time.time < nextMoveTime && currentState != PlayerState.Dead)
        {
            return;
        }
        //默认状态为Idle
        currentState = PlayerState.Idle;

        // 获取输入
        if (!TryGetInputDirection(out Vector2Int direction))
        {
            return;
        }
        
        Vector2Int targetPos = GridPosition + direction;

        //目标格子无效则不行动
        if (!GridManager.IsValidPosition(targetPos))
        {
            return;
        }

        Entity target = GridManager.GetOccupant(targetPos);
        
        // 目标格子有实体则攻击
        if (target != null)
        {
            target.Onhit(direction);
            currentState = PlayerState.Attacking;
            nextMoveTime = Time.time + actCooldown;
            return;
        }

        //否则尝试移动，成功则开始冷却
        if (TryMove(direction))
        {
            currentState = PlayerState.Moving;
            nextMoveTime = Time.time + actCooldown;
        }

    }

    // 获取输入方向（WASD 与方向键）
    private bool TryGetInputDirection(out Vector2Int direction)
    {
        direction = Vector2Int.zero;

        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
        {
            direction = Vector2Int.up; 
            return true;
        }

        if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
        {
            direction = Vector2Int.down;
            return true;
        }

        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
        {
            direction = Vector2Int.left;
            transform.localScale = new Vector3(-1, 1, 1);
            return true;
        }

        if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
        {
            direction = Vector2Int.right;
            transform.localScale = new Vector3(1, 1, 1);
            return true;
        }

        return false;
    }

    override public void Onhit(Vector2Int fromDirection)
    {
        health -= 1;
        DisplayHealth();
        if(health <= 0)
        {
            Die();
            SceneManager.LoadSceneAsync("Lobby",LoadSceneMode.Single);
        }
    }

    public void DisplayHealth()
    {
        // 在屏幕上方显示血量
        if (healthPrefab == null) return;
        for (int i = activeHealthVisuals.Count - 1; i >= 0; i--)
        {
            Destroy(activeHealthVisuals[i]);
        }
        activeHealthVisuals.Clear();
        for (int i = 0; i < health; i++)
        {
            GameObject healthVisual = Instantiate(healthPrefab, new Vector3(-10 + i * 0.8f, 4.5f, 0), Quaternion.identity);
            activeHealthVisuals.Add(healthVisual);
        }
    }

    public override void Die()
    {
        Vector3 deathPosition = transform.position;
        Quaternion deathRotation = transform.rotation;

        base.Die();

        if (deathEffectPrefab != null)
        {
            GameObject effect = Instantiate(deathEffectPrefab, deathPosition, deathRotation);
            if (deathEffectDuration > 0f)
            {
                Destroy(effect, deathEffectDuration);
            }
        }
    }
}
