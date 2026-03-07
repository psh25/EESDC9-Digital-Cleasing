using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LG1_3 : MonoBehaviour
{
    private LaserManager laserManager;
    private int count1;
    private int count2;
    private int cooldown;

    private void Awake()
    {
        // 获取LaserManager组件
        if (laserManager == null)
        {
            laserManager = FindObjectOfType<LaserManager>();
        }
        count1 = 0;
        cooldown = 6;
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
        if (cooldown > 0)
        {
            cooldown--;
            return;
        }
        int executeBeat = BeatManager.BeatIndex + 1;
        if (BeatManager.BeatIndex % 10 == 0)
        {
            count1 += 5;
            count2 += 5;
        }
        if (BeatManager.BeatIndex % 10 >= 0 && BeatManager.BeatIndex % 10 <= 2)
        {
            LaserManager.TryScheduleFullColumnLaser(7 - count1 % 16, executeBeat);
            LaserManager.TryScheduleFullColumnLaser(7 - (count1 + 4) % 16, executeBeat);
            LaserManager.TryScheduleFullColumnLaser(7 - (count1 + 9) % 16, executeBeat);
        }
        else if (BeatManager.BeatIndex % 10 >= 5 && BeatManager.BeatIndex % 10 <= 7)
        {
            LaserManager.TryScheduleFullRowLaser(7 - count2 % 16, executeBeat);
            LaserManager.TryScheduleFullRowLaser(7 - (count2 + 6) % 16, executeBeat);
            LaserManager.TryScheduleFullRowLaser(7 - (count2 + 11) % 16, executeBeat);
        }
    }
}
