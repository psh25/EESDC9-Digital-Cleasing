using UnityEngine;

public class LGTutorial_3 : MonoBehaviour
{
    private LaserManager laserManager;

    private void Awake()
    {
        if (laserManager == null)
        {
            laserManager = FindObjectOfType<LaserManager>();
        }
    }

    private void OnEnable()
    {
        // 订阅节拍事件
        BeatManager.OnBeat += OnBeatTriggered;
    }

    private void OnDisable()
    {
        // 取消订阅，防止内存泄漏或错误调用
        BeatManager.OnBeat -= OnBeatTriggered;
    }

    private void OnBeatTriggered()
    {

        if (BeatManager.BeatIndex % 4 == 0)
        {
            int columnX1 = 1;
            int columnX2 = -1;
            int executeBeat = BeatManager.BeatIndex + 1;
            LaserManager.TryScheduleFullColumnLaser(columnX1, executeBeat);
            LaserManager.TryScheduleFullColumnLaser(columnX2, executeBeat);

        }

    }
}
