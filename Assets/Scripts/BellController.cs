using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BellController : MonoBehaviour
{
    [Header("表现：动画 & 声音（可选）")]
    public Animator playerAnimator;         // 玩家或手部的 Animator（可选）
    public string bellTriggerName = "RingBell";  // Animator 里的 Trigger 名
    public AudioSource bellAudioSource;     // 播放铃声的 AudioSource（可选）

    public GameObject bellObject;
    // Start is called before the first frame update
    void Start()
    {
       
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    // void onawake()
    // {
    //     OnRingBell();
    // }
    public void OnRingBell()
    {
        Debug.Log("Bell rung!");
        // 播放动画（可选）
        if (playerAnimator != null && !string.IsNullOrEmpty(bellTriggerName))
        {
            playerAnimator.SetTrigger(bellTriggerName);
        }

        // 播放铃声音效（可选）
        if (bellAudioSource != null)
        {
            Debug.Log("Playing bell sound");
            bellAudioSource.Play();
        }
        else
        {
            Debug.Log("No AudioSource assigned for bell sound");
        }

    }
}
