using UnityEngine;

public class PlayerBellRinger : MonoBehaviour
{
    [Header("鹿引导脚本")]
    public DeerController deerController;   // 把场景里的 Deer 拖进来

    [Header("输入设置")]
    public KeyCode ringKey = KeyCode.E;     // 摇铃按键（默认 E，可以改）

    [Header("冷却设置")]
    public float ringCooldown = 3.0f;       // 两次摇铃之间的最小间隔
    private float lastRingTime = -999f;

    // [Header("表现：动画 & 声音（可选）")]
    // public Animator playerAnimator;         // 玩家或手部的 Animator（可选）
    // public string bellTriggerName = "RingBell";  // Animator 里的 Trigger 名
    // public AudioSource bellAudioSource;     // 播放铃声的 AudioSource（可选）
    public GameObject bellObject;
    void Start()
    {
        bellObject.SetActive(false);
    }
    void Update()
    {
        // 按下按键就尝试摇铃
        if (Input.GetKeyDown(ringKey))
        {
            TryRingBell();
        }
    }

    void TryRingBell()
    {
        // bellObject.SetActive(true);

        // 冷却未到：直接返回
        if (Time.time - lastRingTime < ringCooldown)
            return;

        // 通知鹿
        if (deerController != null)
        {
            deerController.OnBellRung();
        }

        
        // // 播放动画（可选）
        // if (playerAnimator != null && !string.IsNullOrEmpty(bellTriggerName))
        // {
        //     playerAnimator.SetTrigger(bellTriggerName);
        // }

        // // 播放铃声音效（可选）
        // if (bellAudioSource != null)
        // {
        //     bellAudioSource.Play();
        // }

        lastRingTime = Time.time;
    }
}
