using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// 音频管理器：负责跨场景 BGM 播放，并与 BeatManager 使用同一 DSP 起点对齐节拍
public class AudioManager : MonoBehaviour
{
    // 全局单例，确保场景切换后只有一个音频管理器
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource bgmSource;
    [Header("Battle BGM")]
    [SerializeField] private AudioClip battleBgmClip;
    // 定时播放延迟（秒）：预留调度时间，保证 PlayScheduled 稳定
    [SerializeField] private float scheduleDelaySeconds = 0.1f;
    // 播放战斗 BGM 的支线名
    [SerializeField] private List<string> battleLevelNames = new List<string> { "Tutorial","Game1", "Game2", "Game3", "BossBattle" };

    // 单例初始化并准备 AudioSource
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
            return;
        }

        // 若未手动指定，先尝试复用同物体上的 AudioSource
        if (bgmSource == null)
        {
            bgmSource = GetComponent<AudioSource>();
        }

        // 由代码控制播放时机，默认循环
        bgmSource.playOnAwake = false;
        bgmSource.loop = true;
    }

    // 监听场景加载事件
    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    // 取消监听，避免重复订阅
    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // 启动时主动处理一次当前场景
    private void Start()
    {
        OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    // 场景切换后决定播放或停止战斗 BGM
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        string levelName = scene.name.Split('.')[0];
        if (battleLevelNames.Contains(levelName))
        {
            PlayBattleBgmSynced();
            return;
        }

        StopBgmAndBeat();
    }

    // 使用 DSP 定时启动 BGM，并同步启动 BeatManager
    private void PlayBattleBgmSynced()
    {
        if (battleBgmClip == null)
        {
            return;
        }

        // 已在播放同一首战斗 BGM 时无需重复启动
        if (bgmSource.isPlaying && bgmSource.clip == battleBgmClip)
        {
            return;
        }

        bgmSource.Stop();
        bgmSource.clip = battleBgmClip;
        bgmSource.loop = true;

        // 统一 DSP 起点：音频和节拍都从这里开始
        double songStartDsp = AudioSettings.dspTime + scheduleDelaySeconds;
        bgmSource.PlayScheduled(songStartDsp);

        // 使用同一时间基准启动节拍，保证对拍
        if (BeatManager.Instance != null)
        {
            BeatManager.Instance.StartSong(songStartDsp);
        }
    }

    // 停止音乐并同步停止节拍
    private void StopBgmAndBeat()
    {
        if (bgmSource.isPlaying)
        {
            bgmSource.Stop();
        }

        if (BeatManager.Instance != null)
        {
            BeatManager.Instance.StopSong();
        }
    }
}
