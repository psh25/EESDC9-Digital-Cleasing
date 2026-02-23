using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BeatManager : MonoBehaviour
{
    public float bpm = 120f;
    public float gametime = 0f;
    private float beatInterval;
    private float beatTimer = 0f;
    public static int BeatIndex { get; private set; } = 0;
    public static event System.Action OnBeatStart;
    public static event System.Action OnBeat;

    // Start is called before the first frame update
    void Start()
    {
        beatInterval = 60f / bpm;//计算每拍的时间间隔
        BeatIndex = 0;
    }

    // Update is called once per frame
    void Update()
    {
        beatTimer += Time.deltaTime;
        gametime += Time.deltaTime;
        if (beatTimer >= beatInterval)
        {
            beatTimer -= beatInterval;

            // 更新拍号并广播拍开始
            BeatIndex++;
            OnBeatStart?.Invoke();

            // 触发OnBeat事件
            OnBeat?.Invoke();
        }
    }
}
