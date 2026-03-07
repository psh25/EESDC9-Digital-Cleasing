using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class Player : Entity
{
    [Header("Move Settings")]
    [SerializeField] public float actCooldown = 0.1f;
    [SerializeField] private float baseBpm = 120f;
    public bool isReverseDirection = false;
    public bool isProtected = false;

    [SerializeField] private GameObject healthPrefab;
    public int health = 3;
    private readonly List<GameObject> activeHealthVisuals = new List<GameObject>();

    [Header("Health UI Settings")]
    // 视口坐标锚点：左下(0,0) 右上(1,1)，默认贴近左上角。
    [SerializeField] private Vector2 healthAnchorViewport = new Vector2(0.06f, 0.90f);
    // 相邻血量图标在视口坐标中的水平间距。
    [SerializeField] private float healthSpacingViewport = 0.04f;
    // 图标最终放置的世界坐标 Z 平面。
    [SerializeField] private float healthPlaneZ = 0f;
    // 正交相机参考尺寸，用于保持图标相对屏幕大小稳定。
    [SerializeField] private float healthReferenceOrthoSize = 5f;

    private Camera healthDisplayCamera;
    private Vector3 healthBaseScale = Vector3.one;

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

        healthDisplayCamera = Camera.main;
        if (healthPrefab != null)
        {
            healthBaseScale = healthPrefab.transform.localScale;
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

        if(isReverseDirection)
        {
            direction = -direction;   // 反转输入方向
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
            Debug.Log($"Player moved to {GridPosition}");
        }

    }

    private void LateUpdate()
    {
        // 放在 LateUpdate，确保本帧相机移动/缩放后再更新 UI 位置。
        UpdateHealthVisualLayout();
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

        if (Input.GetKeyDown(KeyCode.R))
        {
            Die();
        }

        return false;
    }

    override public void Onhit(Vector2Int fromDirection)
    {
        if (isProtected)
        {
            // Todo:展示护盾碎裂的动画或特效
            isProtected = false; // 保护状态只持续一次
            return;
        }
        health -= 1;
        DisplayHealth();
        if(health == 0)
        {
            Die();
        }
    }

    public void DisplayHealth()
    {
        if (healthPrefab == null) return;

        for (int i = activeHealthVisuals.Count - 1; i >= 0; i--)
        {
            Destroy(activeHealthVisuals[i]);
        }

        activeHealthVisuals.Clear();

        for (int i = 0; i < health; i++)
        {
            // 先生成，再统一按相机视口进行布局。
            GameObject healthVisual = Instantiate(healthPrefab, Vector3.zero, Quaternion.identity);
            activeHealthVisuals.Add(healthVisual);
        }

        UpdateHealthVisualLayout();
    }

    private Camera GetHealthDisplayCamera()
    {
        if (healthDisplayCamera == null)
        {
            healthDisplayCamera = Camera.main;
        }

        return healthDisplayCamera;
    }

    private void UpdateHealthVisualLayout()
    {
        if (activeHealthVisuals.Count == 0)
        {
            return;
        }

        Camera displayCamera = GetHealthDisplayCamera();
        if (displayCamera == null)
        {
            return;
        }

        // 将显示平面与相机的距离转换为 ViewportToWorldPoint 所需的 z 参数。
        float zDistance = healthPlaneZ - displayCamera.transform.position.z;
        if (zDistance >= 0f)
        {
            zDistance = Mathf.Abs(zDistance) + 0.01f;
        }

        Vector3 scaledHealthSize = healthBaseScale;
        if (displayCamera.orthographic && healthReferenceOrthoSize > 0.01f)
        {
            // 正交相机尺寸改变时，按比例补偿图标缩放，保持屏幕观感大小基本不变。
            scaledHealthSize *= displayCamera.orthographicSize / healthReferenceOrthoSize;
        }

        for (int i = 0; i < activeHealthVisuals.Count; i++)
        {
            GameObject healthVisual = activeHealthVisuals[i];
            if (healthVisual == null)
            {
                continue;
            }

            float viewportX = healthAnchorViewport.x + i * healthSpacingViewport;
            // 将“相对屏幕位置”转换到世界坐标，实现始终贴在左上区域。
            Vector3 worldPosition = displayCamera.ViewportToWorldPoint(new Vector3(viewportX, healthAnchorViewport.y, zDistance));
            worldPosition.z = healthPlaneZ;

            healthVisual.transform.position = worldPosition;
            healthVisual.transform.localScale = scaledHealthSize;
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
        if ((SceneManager.GetActiveScene().name).Split('.')[0] != "Tutorial")
        {
            SceneManager.LoadSceneAsync("Lobby", LoadSceneMode.Single);
        }
        else
        {
            SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().name, LoadSceneMode.Single);
        }
    }
}
