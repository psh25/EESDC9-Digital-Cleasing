using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Portal : Entity
{
    [SerializeField]private string nextSceneName;  //下一场景名称
    private Animator animator;
    public GameStateManager gameStateManager;
    [SerializeField]private bool active = false;    //是否激活
    private string currentSceneName;
    private string currentLevel;
    private string nextLevel;

    public override void Awake()
    {
        base.Awake();
        if (animator == null) //获取Animator组件
        {
            animator = GetComponent<Animator>();
        }
        if (gameStateManager == null) //获取GameStateManager组件
        {
            gameStateManager = FindObjectOfType<GameStateManager>();
        }

        currentSceneName = SceneManager.GetActiveScene().name;  //获取当前场景名称
        currentLevel = currentSceneName.Split('.')[0];  //提取当前关卡（Game1, Game2, Game3, Lobby等）
        nextLevel = nextSceneName.Split('.')[0];  //提取下一关卡名称
    }

    private void Update()
    {
        if (currentSceneName == "Lobby")  //如果在Lobby,根据GameStateManager中的LevelAccess字典设置传送门状态
        {
            active = gameStateManager.LevelAccess[nextLevel];
            animator.SetBool("Active", active); //更新传送门状态显示
        }
        
        else if (!active)//小关内持续检查是否完成关卡
        {
            CheckCompletion();
            animator.SetBool("Active", active);  
        }
    }

    private void CheckCompletion()    //检查是否完成关卡(小关)
    {
        if (GridManager == null)
        {
            return;
        }

        // 遍历Tilemap上的有效格子
        foreach (Vector2Int checkPos in GridManager.GetValidPositions())
        {
            if (GridManager.GetOccupant(checkPos) is Boss || GridManager.GetOccupant(checkPos) is Firewall)
            {
                active = false;  //如果还有Boss或Firewall，关卡未完成
                return;
            }
        }
        active = true;
    }


    public override void Onhit(Vector2Int attackDirection)
    {
        if (active == true)
        {
            if (gameStateManager != null && nextSceneName == "Lobby")//支线完成后重设关卡访问权限
            {
                gameStateManager.LevelAccess[currentLevel] = false;
                if (gameStateManager.LevelAccess["Game1"] == false && gameStateManager.LevelAccess["Game2"] == false && gameStateManager.LevelAccess["Game3"] == false) //如果所有小关都完成了，解锁Boss关
                {
                    gameStateManager.LevelAccess["GameBoss"] = true;
                }
            }
            SceneManager.LoadSceneAsync(nextSceneName,LoadSceneMode.Single);  //加载下一关场景
        }
        else
        {
            Debug.Log("关卡未完成");          //关卡未完成，不能进入下一关
            return;
        }
    }
}
