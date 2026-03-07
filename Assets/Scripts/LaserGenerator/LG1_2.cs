using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LG1_2 : MonoBehaviour
{
    private LaserManager laserManager;
    private int count1;
    private int cooldown;

    private void Awake()
    {
        // 获取LaserManager组件
        if (laserManager == null)
        {
            laserManager = FindObjectOfType<LaserManager>();
        }
        count1 = 0;
        cooldown = 5;
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
        if (BeatManager.BeatIndex % 2 == 0) return;
        count1++;
        int executeBeat = BeatManager.BeatIndex + 1;
        LaserManager.TryScheduleFullColumnLaser(6 - count1 % 15, executeBeat);
        LaserManager.TryScheduleFullColumnLaser(6 - (count1 + 5) % 15, executeBeat);
        LaserManager.TryScheduleFullColumnLaser(6 - (count1 + 10) % 15, executeBeat);
        
    }
}
