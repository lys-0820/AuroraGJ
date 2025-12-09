using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class DeerSegmentData
{
    public Transform spawnPoint;      // 鹿出现点（即当前 waypoint）
    public Transform vanishPoint;     // 鹿消失点（朝下一点方向偏移）
    public Transform[] runWaypoints;  // 鹿跑动路径：这里就只用 vanishPoint 即可
}

public class DeerRoutePlanner : MonoBehaviour
{
    [Header("主路线（你已经在场景里摆好的空物体）")]
    public MainPath mainPath;          // 里面有 List<Transform> waypoints

    [Header("鹿从当前点朝下一个点跑多远")]
    public float runDistance = 3f;     // “跑两步”的距离，自己感受调整

    [Header("自动生成的分段数据")]
    public DeerSegmentData[] segments;

    void Start()
    {
        GenerateSegments();
    }

    void GenerateSegments()
    {
        if (mainPath == null || mainPath.waypoints.Count < 2)
        {
            Debug.LogError("MainPath 未设置或路径点少于 2 个。");
            return;
        }

        List<Transform> path = mainPath.waypoints;
        int count = path.Count;

        // 一共生成 count-1 段：从第 0 个点 → 第 1 段提示 … → 倒数第二 → 最后一个
        segments = new DeerSegmentData[count - 1];

        for (int i = 0; i < count - 1; i++)
        {
            Transform cur = path[i];
            Transform next = path[i + 1];

            // 1）出现点 = 当前 waypoint
            Transform spawn = cur;

            // 2）计算朝向下一个点的方向，在这个方向上偏移 runDistance 作为消失点
            Vector3 dir = (next.position - cur.position);
            dir.y = 0f;
            dir.Normalize();

            Vector3 vanishPos = cur.position + dir * runDistance;

            GameObject vanishGO = new GameObject($"DeerVanish_{i}");
            vanishGO.transform.position = vanishPos;
            vanishGO.transform.parent = this.transform;

            // 3）这一段鹿的跑动路径：就只有一个目标点（vanishPoint）
            Transform[] runPoints = new Transform[1];
            runPoints[0] = vanishGO.transform;

            segments[i] = new DeerSegmentData
            {
                spawnPoint = spawn,
                vanishPoint = vanishGO.transform,
                runWaypoints = runPoints
            };
        }

        Debug.Log($"DeerRoutePlanner: 生成了 {segments.Length} 段鹿的路线。");
    }
}
