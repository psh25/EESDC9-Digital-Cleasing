using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// 传送门：负责显示激活状态、检测关卡完成并切换场景
public class Portal : Entity
{
    // 目标场景名称
    [SerializeField]private string nextSceneName;

    // 门的动画组件
    private Animator animator;
    // 全局关卡状态管理器引用
    private MessageManager messageManager;
    public GameStateManager gameStateManager;

    // 门当前是否可用
    [SerializeField]private bool active = false;

    // 当前场景名/当前关卡键/目标关卡键
    private string currentSceneName;
    private string currentLevel;
    private string nextLevel;

    // 初始化组件与场景关键信息
    public override void Awake()
    {
        base.Awake();
        messageManager = FindObjectOfType<MessageManager>();
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        // 优先使用单例，失败再回退场景查找
        if (gameStateManager == null)
        {
            gameStateManager = GameStateManager.Instance;
            if (gameStateManager == null)
            {
                gameStateManager = FindObjectOfType<GameStateManager>();
            }
        }

        // 记录当前场景与目标场景的关卡键（兼容可能带扩展名的写法）
        currentSceneName = SceneManager.GetActiveScene().name;
        currentLevel = currentSceneName.Split('.')[0];
        nextLevel = nextSceneName.Split('.')[0];
    }

    // Lobby 中根据关卡状态更新门开关；子关中检查是否达成通关条件
    private void Update()
    {
        if (currentSceneName == "Lobby")
        {
            active = gameStateManager != null && gameStateManager.IsLevelAccessible(nextLevel);
            animator.SetBool("Active", active);
        }
        
        else if (!active)
        {
            CheckCompletion();
            animator.SetBool("Active", active);  
        }
    }

    // 小关通关检测：地图上不存在 Boss/Firewall 则视为完成
    private void CheckCompletion()
    {
        if (GridManager == null)
        {
            return;
        }

        // 遍历 Tilemap 有效格，若仍有关键敌人则未完成
        foreach (Vector2Int checkPos in GridManager.GetValidPositions())
        {
            if (GridManager.GetOccupant(checkPos) is Enemy || GridManager.GetOccupant(checkPos) is Firewall)
            {
                active = false;
                return;
            }
        }
        active = true;
    }


    // 被攻击触发传送：激活时切场景；从子关返回 Lobby 时更新关卡完成状态
    public override void Onhit(Vector2Int attackDirection)
    {
        if (nextSceneName == "BossBattle" && gameStateManager != null && gameStateManager.LevelAccess["BossBattle"] == false)
        {
            messageManager.ShowMessage("这个传送门后封印着强大的Boss，完成所有关卡以解除封印！");
        }
        if (active == true)
        {
            if (gameStateManager != null && nextSceneName == "Lobby")
            {
                gameStateManager.MarkLevelCompleted(currentLevel);
            }
            SceneManager.LoadSceneAsync(nextSceneName, LoadSceneMode.Single);
        }
        else
        {
            Debug.Log("关卡未完成");
            return;
        }
    }
}
