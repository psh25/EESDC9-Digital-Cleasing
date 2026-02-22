using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LaserGenerator1 : MonoBehaviour
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
            int rowY = 1;
            int executeBeat = BeatManager.BeatIndex + 1;
            LaserManager.TryScheduleFullRowLaser(rowY, executeBeat);
        }
        
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
