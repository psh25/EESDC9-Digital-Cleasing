using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LG1_1 : MonoBehaviour
{
    private LaserManager laserManager;
    private int countX;
    private int countY;

    private void Awake()
    {
        if (laserManager == null)
        {
            laserManager = FindObjectOfType<LaserManager>();
        }
        countX = 0;
        countY = 0;
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
        int executeBeat = BeatManager.BeatIndex + 1;
        if (BeatManager.BeatIndex % 4 == 0)
        {
            countY++;
            LaserManager.TryScheduleFullRowLaser(3 - countY % 8, executeBeat);
            LaserManager.TryScheduleFullRowLaser(countY % 8 - 4, executeBeat);

        }
        else if(BeatManager.BeatIndex % 4 == 2)
        {
            countX++;
            LaserManager.TryScheduleFullColumnLaser(3 - countX % 8, executeBeat);
            LaserManager.TryScheduleFullColumnLaser(countX % 8 - 4, executeBeat);
        }   
    }
}
