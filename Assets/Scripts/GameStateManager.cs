using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance;
    [SerializeField]public Dictionary<string, bool> LevelAccess = new Dictionary<string, bool>();

    private void Awake()// 确保单例模式,场景切换时数据不丢失
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

    // Start is called before the first frame update
    void Start()
    {
        if(LevelAccess.Count == 0) //首次启动游戏时初始化关卡访问权限
        {
            InitializeLevelAccess();
        }
    }

    void InitializeLevelAccess()
    {
        LevelAccess.Add("Game1", true);
        LevelAccess.Add("Game2", true);   
        LevelAccess.Add("Game3", true);
        LevelAccess.Add("GameBoss", false);
    }
}
