using UnityEngine;

public class HandBob : MonoBehaviour
{
    public Transform player;       // 拖 Player 或 Camera（能代表移动即可）
    public float bobSpeed = 6f;    // 摆动速度
    public float bobAmount = 0.03f;// 摆动幅度
    public float moveThreshold = 0.01f; // 低于这个速度视为静止

    private Vector3 startPos;
    private Vector3 lastPlayerPos;

    void Start()
    {
        startPos = transform.localPosition;
        if (player != null)
            lastPlayerPos = player.position;
    }

    void Update()
    {
        if (player == null)
            return;

        // 计算玩家速度（帧间移动距离）
        float speed = (player.position - lastPlayerPos).magnitude / Time.deltaTime;
        lastPlayerPos = player.position;

        // 是否在移动？
        bool isMoving = speed > moveThreshold;

        if (isMoving)
        {
            float offset = Mathf.Sin(Time.time * bobSpeed) * bobAmount;
            transform.localPosition = startPos + new Vector3(0, offset, 0);
        }
        else
        {
            // 回到原位
            transform.localPosition = Vector3.Lerp(
                transform.localPosition,
                startPos,
                Time.deltaTime * 8f
            );
        }
    }
}
