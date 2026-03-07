using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LGModel : MonoBehaviour
{
    private LaserManager laserManager;

    private void Awake()
    {
        // 获取LaserManager组件
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
        
    }
}
