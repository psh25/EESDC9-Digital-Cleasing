using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 游戏状态管理器：跨场景维护关卡可进入状态与解锁规则
public class GameStateManager : MonoBehaviour
{
    // 全局单例
    public static GameStateManager Instance;
    private MessageManager messageManager;
    // 关卡访问状态：true=可进入，false=不可进入/已完成后关闭入口
    [SerializeField]public Dictionary<string, bool> LevelAccess = new Dictionary<string, bool>();

    // Boss 关卡键名
    private const string BossLevelName = "BossBattle";
    // 参与 Boss 解锁判定的主线关卡
    private static readonly string[] MainLevels = { "Game1", "Game2", "Game3" };

    // 确保单例模式，场景切换时数据不丢失
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // 首次进入游戏时初始化关卡状态
    void Start()
    {
        messageManager = FindObjectOfType<MessageManager>();
        if (LevelAccess.Count == 0) //首次启动游戏时初始化关卡访问权限
        {
            InitializeLevelAccess();
        }
    }

    // 初始化关卡访问状态
    void InitializeLevelAccess()
    {
        LevelAccess.Clear();
        LevelAccess["Game1"] = true;
        LevelAccess["Game2"] = true;
        LevelAccess["Game3"] = true;
        LevelAccess[BossLevelName] = false; // Boss关初始可进入，调试用
    }

    // 查询某关卡当前是否可进入（带空值保护）
    public bool IsLevelAccessible(string levelName)
    {
        if (string.IsNullOrEmpty(levelName))
        {
            return false;
        }

        return LevelAccess.TryGetValue(levelName, out bool accessible) && accessible;
    }

    // 标记某关卡完成，并尝试更新 Boss 解锁状态
    public void MarkLevelCompleted(string levelName)
    {
        if (string.IsNullOrEmpty(levelName))
        {
            return;
        }

        if (LevelAccess.ContainsKey(levelName))
        {
            LevelAccess[levelName] = false;
        }

        TryUnlockBossLevel();
    }

    // 当主线关卡都完成后，解锁 Boss 关
    private void TryUnlockBossLevel()
    {
        bool allMainLevelsCompleted = true;

        foreach (string levelName in MainLevels)
        {
            if (!LevelAccess.TryGetValue(levelName, out bool accessible) || accessible)
            {
                allMainLevelsCompleted = false;
                break;
            }
        }

        if (allMainLevelsCompleted)
        {
            LevelAccess[BossLevelName] = true;
            if (messageManager != null)
            {
                messageManager.ShowMessage("Boss关已解锁！");
            }
        }
    }
}
