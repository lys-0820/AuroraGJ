using UnityEngine;

public class HandFollowLook : MonoBehaviour
{
    public Transform cam;           // 拖主摄像机
    public float rotateLerp = 12f;  // 旋转跟随速度
    public float moveLerp = 8f;     // 位置插值速度

    public Vector3 baseLocalPos = new Vector3(0.3f, -0.3f, 0.6f); // 手的默认局部位置
    public Vector3 lookOffset = new Vector3(0f, -0.05f, -0.05f);  // 抬头/低头时额外偏移
    void LateUpdate()
    {
        if (!cam) return;

        // 1）旋转跟随相机
        transform.rotation = Quaternion.Lerp(
            transform.rotation,
            cam.rotation,
            rotateLerp * Time.deltaTime
        );

        // 2）根据相机 pitch 做一点上下/前后位移
        float pitch = cam.localEulerAngles.x;
        if (pitch > 180f) pitch -= 360f; // 把 0~360 转成 -180~180

        // 假设 -60°~60° 这个范围内做插值
        float t = Mathf.InverseLerp(-60f, 60f, pitch);   // 映射到 0~1
        float centered = (t - 0.5f) * 2f;                // 映射到 -1~1

        Vector3 targetPos = baseLocalPos + lookOffset * centered;

        transform.localPosition = Vector3.Lerp(
            transform.localPosition,
            targetPos,
            moveLerp * Time.deltaTime
        );
    }
}
