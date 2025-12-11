using UnityEngine;
public class RandomAuroraSound : MonoBehaviour
{
    public AudioSource auroraAudioSource; // 挂在极光上的 AudioSource
    public AudioClip[] auroraClips;       // 极光播放的音效列表
    public float minInterval = 5f;        // 最小播放间隔
    public float maxInterval = 15f;       // 最大播放间隔

    private float nextPlayTime = 3f;

    void Update()
    {
        if (Time.time >= nextPlayTime)
        {
            PlayRandomAuroraSound();
            ScheduleNextPlay();
        }
    }

    void PlayRandomAuroraSound()
    {
        if (auroraAudioSource == null || auroraClips == null || auroraClips.Length == 0)
            return;

        int index = Random.Range(0, auroraClips.Length);
        AudioClip clip = auroraClips[index];
        auroraAudioSource.PlayOneShot(clip);
    }

    void ScheduleNextPlay()
    {
        float interval = Random.Range(minInterval, maxInterval);
        nextPlayTime = Time.time + interval;
    }
}