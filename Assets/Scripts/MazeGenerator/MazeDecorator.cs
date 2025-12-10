using System.Collections.Generic;
using UnityEngine;

public class MazeDecorator : MonoBehaviour
{
    [Header("References")]
    public MazeBuilder mazeBuilder;      // 用来拿 cell 世界坐标 & cellSize

    [Header("Decoration Prefabs")]
    [Tooltip("路中间的小东西，比如石头、草丛、木桩等")]
    public GameObject[] propPrefabs;

    [Header("Placement Settings")]
    [Tooltip("每个格子放装饰的概率（0~1）")]
    [Range(0f, 1f)]
    public float propChancePerCell = 0.4f;

    [Tooltip("单个格子最多放多少个装饰")]
    public int maxPropsPerCell = 2;

    [Tooltip("装饰在格子中心附近的随机范围（相对于 cellSize 的比例）")]
    [Range(0f, 0.5f)]
    public float cellJitterFactor = 0.3f;

    [Tooltip("装饰之间的最小水平距离（XZ），避免重叠")]
    public float minPropDistance = 0.7f;

    [Header("Ground / Raycast")]
    [Tooltip("哪些层作为地面，用于射线检测高度")]
    public LayerMask groundLayer;

    [Tooltip("从上方向下射线的高度")]
    public float raycastHeight = 5f;

    [Tooltip("装饰物底部离地面的偏移")]
    public float groundOffset = 0f;

    private readonly List<GameObject> spawnedProps = new List<GameObject>();
    private readonly List<Vector3> placedPositions = new List<Vector3>();

    /// <summary>
    /// 对给定的迷宫数据进行装饰（在路径中间放小东西）。
    /// </summary>
    public void Decorate(MazeData data)
    {
        ClearProps();

        if (data == null || mazeBuilder == null)
        {
            Debug.LogWarning("MazeDecorator.Decorate: data 或 mazeBuilder 为空。");
            return;
        }

        if (propPrefabs == null || propPrefabs.Length == 0)
        {
            // 没有装饰 prefab，直接跳过
            return;
        }

        placedPositions.Clear();

        float cellSize = mazeBuilder.cellSize;
        float jitterRadius = cellSize * cellJitterFactor;

        for (int x = 0; x < data.width; x++)
        {
            for (int y = 0; y < data.height; y++)
            {
                MazeCell cell = data.cells[x, y];

                // 起点/终点可以选择不放（避免挡路），你也可以删掉这两行保留装饰
                if (cell == data.startCell || cell == data.endCell)
                    continue;

                // 随机决定这一格要不要装饰
                if (Random.value > propChancePerCell)
                    continue;

                // 这一格要放多少个（1..max）
                int count = Mathf.Max(1, Random.Range(1, maxPropsPerCell + 1));

                Vector3 cellCenter = mazeBuilder.GetWorldPosOfCell(cell);

                for (int i = 0; i < count; i++)
                {
                    // 在 cell 中心附近做一点随机偏移（保证不靠近树墙）
                    Vector2 jitter2D = Random.insideUnitCircle * jitterRadius;
                    Vector3 flatPos = cellCenter + new Vector3(jitter2D.x, 0f, jitter2D.y);

                    // 用射线贴到地面
                    Vector3 groundPos = SnapToGround(flatPos);
                    Vector3 finalPos = groundPos + Vector3.up * groundOffset;

                    // 检查与已有装饰的距离，太近就放弃这一个
                    if (!IsFarEnoughFromOthers(finalPos))
                        continue;

                    GameObject prefab = GetRandomPropPrefab();
                    if (prefab == null)
                        continue;

                    // 随机一个旋转，让小东西不要太整齐
                    Quaternion rot = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

                    GameObject obj = Instantiate(prefab, finalPos, rot, transform);
                    spawnedProps.Add(obj);
                    placedPositions.Add(finalPos);
                }
            }
        }
    }

    /// <summary>
    /// 清理之前生成的所有装饰。
    /// </summary>
    public void ClearProps()
    {
        foreach (var obj in spawnedProps)
        {
            if (obj != null)
                Destroy(obj);
        }
        spawnedProps.Clear();
        placedPositions.Clear();
    }

    private GameObject GetRandomPropPrefab()
    {
        if (propPrefabs == null || propPrefabs.Length == 0)
            return null;
        int idx = Random.Range(0, propPrefabs.Length);
        return propPrefabs[idx];
    }

    /// <summary>
    /// 利用 RaycastAll 找地面，忽略自身子物体，避免打到装饰或树上。
    /// </summary>
    private Vector3 SnapToGround(Vector3 flatPosition)
    {
        Vector3 origin = flatPosition + Vector3.up * raycastHeight;
        Ray ray = new Ray(origin, Vector3.down);

        RaycastHit[] hits = Physics.RaycastAll(ray, raycastHeight * 2f, groundLayer, QueryTriggerInteraction.Ignore);

        if (hits != null && hits.Length > 0)
        {
            float lowestY = float.MaxValue;
            bool foundGround = false;

            foreach (var hit in hits)
            {
                // 排除 MazeDecorator 自己生成的物体（都是它的子物体）
                if (hit.collider.transform.IsChildOf(transform))
                    continue;

                // 也顺便排掉 MazeBuilder 生成的树：它可能是另一个父物体，你可以按 layer 控，如果你把树设成非 groundLayer，就不会被打中
                if (hit.point.y < lowestY)
                {
                    lowestY = hit.point.y;
                    foundGround = true;
                }
            }

            if (foundGround)
            {
                return new Vector3(flatPosition.x, lowestY, flatPosition.z);
            }
        }

        return flatPosition;
    }

    /// <summary>
    /// 检查 finalPos 与已放装饰的水平距离是否足够远。
    /// </summary>
    private bool IsFarEnoughFromOthers(Vector3 pos)
    {
        if (minPropDistance <= 0f || placedPositions.Count == 0)
            return true;

        float minSqr = minPropDistance * minPropDistance;

        foreach (var p in placedPositions)
        {
            float dx = pos.x - p.x;
            float dz = pos.z - p.z;
            float sqr = dx * dx + dz * dz;
            if (sqr < minSqr)
                return false;
        }

        return true;
    }
}
