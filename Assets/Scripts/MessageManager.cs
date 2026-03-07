using UnityEngine;
using TMPro;               // 如果用 TextMeshPro，取消注释

public class MessageManager : MonoBehaviour
{
    [SerializeField] private float displayDuration = 2f; // 显示时长（秒）
    private TMP_Text messageText;
    private Coroutine hideCoroutine;

    private void Awake()
    {
        messageText = GetComponent<TMP_Text>();
        gameObject.SetActive(true); // 确保初始隐藏
    }

    public void ShowMessage(string content)
    {
        ShowMessage(content, displayDuration);
    }

    public void ShowMessage(string content, float duration)
    {
        messageText.text = content;
        gameObject.SetActive(true);
        Debug.Log("文本已生成");

        // 停止之前的协程，防止冲突
        if (hideCoroutine != null)
            StopCoroutine(hideCoroutine);

        hideCoroutine = StartCoroutine(HideAfterDelay(duration));
    }

    private System.Collections.IEnumerator HideAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        gameObject.SetActive(false);
    }
}
