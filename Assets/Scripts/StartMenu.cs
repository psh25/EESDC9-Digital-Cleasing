using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI; 

public class StartMenuController : MonoBehaviour
{
    public Button startButton;
    public Button settingsButton;
    public Button exitButton;

    public string mainGameSceneName = "IntroductionScene"; // 游戏主场景名称


       // 关键：Unity会在脚本启动时自动调用Start()方法
    void Start()
    {
        // 检查按钮是否在Inspector中被正确关联，防止空引用
        if (startButton != null)
        {
            // 核心：将OnStartButtonClicked函数“添加”为startButton的点击监听器
            startButton.onClick.AddListener(OnStartButtonClicked);
        }
        else
        {
            Debug.LogError("StartMenuController: startButton 未在Inspector中分配！");
        }

        if (settingsButton != null)
        {
            settingsButton.onClick.AddListener(OnSettingsButtonClicked);
        }

        if (exitButton != null)
        {
            exitButton.onClick.AddListener(OnExitButtonClicked);
        }
    }
    // 4. 开始游戏按钮的功能
    void OnStartButtonClicked()
    {
        Debug.Log("开始任务指令已接收。");
        // TODO:加载游戏主场景
             // 2. 检查场景名变量是否为空或未赋值（防止因未设置导致的错误）
        if (!string.IsNullOrEmpty("IntroductionScene"))
        {
            // 3. 核心：执行场景切换
            SceneManager.LoadSceneAsync("IntroductionScene", LoadSceneMode.Single);
        }
        else
        {
            // 4. 错误处理：如果场景名未设置，在控制台输出醒目错误
        Debug.LogError("StartMenuController: 主场景名称未设置！请检查Inspector中 mainGameSceneName 变量。");
        }
    }

    // 5. 设置按钮的功能（示例：打开/关闭设置面板）
    void OnSettingsButtonClicked()
    {
        Debug.Log("打开系统设置。");
        // 这里可以写打开设置面板的逻辑
    }

    // 6. 退出游戏按钮的功能
    void OnExitButtonClicked()
    {
        Debug.Log("终止程序。");
        // 在Unity编辑器里停止播放
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            // 在打包出的游戏里退出应用
            Application.Quit();
        #endif
    }
}
