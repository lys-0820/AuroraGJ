using System.Collections.Generic;
using UnityEngine;

public class MazeBuilder : MonoBehaviour
{
    [Header("Prefabs")]
    [Tooltip("用作墙的树 Prefab 列表，每次生成墙时会随机选一个")]
    public GameObject[] wallPrefabs;

    [Tooltip("出口 Prefab")]
    public GameObject exitPrefab;

    [Header("Layout")]
    [Tooltip("每个格子的间距（决定迷宫整体大小）")]
    public float cellSize = 2f;

    [Tooltip("是否按 cellSize / wallHeight / wallThickness 缩放树（一般树不需要，默认关掉）")]
    public bool scaleWalls = false;

    [Tooltip("缩放用：墙的高度")]
    public float wallHeight = 2f;

    [Tooltip("缩放用：墙的厚度")]
    public float wallThickness = 0.2f;

    [Header("Tree Strip Settings")]
    [Tooltip("每一段墙上生成多少棵树（>=1）")]
    public int treesPerSegment = 3;

    [Tooltip("相对于 cellSize 的通道宽度系数（0~1，数值越小路越窄）")]
    [Range(0.2f, 1.0f)]
    public float pathWidthFactor = 0.6f;

    [Header("Organic Jitter")]
    [Tooltip("沿着墙方向的随机抖动幅度（米）")]
    public float positionJitterAlong = 0.3f;

    [Tooltip("垂直墙方向的随机抖动幅度（米）")]
    public float positionJitterPerp = 0.5f;

    [Tooltip("树在 Y 轴上的随机旋转角度范围（±度数）")]
    public float rotationJitterY = 15f;

    [Header("Ground / Raycast Settings")]
    [Tooltip("哪些层作为地面，用于射线检测高度")]
    public LayerMask groundLayer;

    [Tooltip("从上方向下射线的高度（相对于墙的平面位置）")]
    public float raycastHeight = 10f;

    [Tooltip("树底部离地面的偏移（比如 0.0 或 0.1）")]
    public float groundOffset = 0.0f;

    [Header("Spacing Control")]
    [Tooltip("树之间的最小水平距离（XZ），防止堆叠")]
    public float minTreeDistance = 0.8f;

    private List<GameObject> spawnedObjects = new List<GameObject>();
    private List<Vector3> placedTreePositions = new List<Vector3>();  // 记录已放树的位置，用于间距检查
    private MazeData currentData;

    public void Build(MazeData data)
    {
        if (data == null)
        {
            Debug.LogError("MazeBuilder.Build: MazeData is null!");
            return;
        }

        currentData = data;
        placedTreePositions.Clear();

        for (int x = 0; x < data.width; x++)
        {
            for (int y = 0; y < data.height; y++)
            {
                MazeCell cell = data.cells[x, y];
                Vector3 cellWorld = CellToWorld(cell);

                // 当前设置下的“半通道宽”和“墙段长度”
                float halfGap = (cellSize * pathWidthFactor) * 0.5f;
                float segmentLength = cellSize * pathWidthFactor;

                // 上墙（北）：沿 X 方向一条线
                if (cell.walls[0])
                {
                    Vector3 center = cellWorld + new Vector3(0f, 0f, halfGap);
                    Vector3 dir = Vector3.right;             // 沿 X 轴排树
                    Quaternion rot = Quaternion.identity;
                    SpawnWallStrip(center, dir, segmentLength, rot);
                }

                // 右墙（东）：沿 Z 方向一条线
                if (cell.walls[1])
                {
                    Vector3 center = cellWorld + new Vector3(halfGap, 0f, 0f);
                    Vector3 dir = Vector3.forward;           // 沿 Z 轴排树
                    Quaternion rot = Quaternion.Euler(0f, 90f, 0f);
                    SpawnWallStrip(center, dir, segmentLength, rot);
                }

                // 下墙（南）—— 只在最底一行生成，避免重复
                if (y == 0 && cell.walls[2])
                {
                    Vector3 center = cellWorld + new Vector3(0f, 0f, -halfGap);
                    Vector3 dir = Vector3.right;
                    Quaternion rot = Quaternion.identity;
                    SpawnWallStrip(center, dir, segmentLength, rot);
                }

                // 左墙（西）—— 只在最左一列生成，避免重复
                if (x == 0 && cell.walls[3])
                {
                    Vector3 center = cellWorld + new Vector3(-halfGap, 0f, 0f);
                    Vector3 dir = Vector3.forward;
                    Quaternion rot = Quaternion.Euler(0f, 90f, 0f);
                    SpawnWallStrip(center, dir, segmentLength, rot);
                }
            }
        }

        // 出口：同样贴地
        if (exitPrefab != null && data.endCell != null)
        {
            Vector3 exitBasePos = CellToWorld(data.endCell);
            Vector3 exitPos = SnapToGround(exitBasePos) + Vector3.up * 0.5f;
            GameObject exitObj = Instantiate(exitPrefab, exitPos, Quaternion.identity, transform);
            spawnedObjects.Add(exitObj);
        }
    }

    /// <summary>
    /// 在一段墙上沿着 dir 排成一排树，并加一些随机抖动。
    /// center: 墙中心点（平面）
    /// dir: 单位方向（例如 Vector3.right 或 Vector3.forward）
    /// length: 墙长度（一般等于 cellSize * pathWidthFactor）
    /// </summary>
    private void SpawnWallStrip(Vector3 center, Vector3 dir, float length, Quaternion baseRotation)
    {
        if (treesPerSegment <= 0)
            treesPerSegment = 1;

        float step = length / treesPerSegment;

        // 垂直方向（平面内） = dir 旋转 90 度
        Vector3 perp = new Vector3(-dir.z, 0f, dir.x).normalized;

        for (int i = 0; i < treesPerSegment; i++)
        {
            // 基本均匀分布在 [-length/2, +length/2]
            float offsetAlong = -length * 0.5f + step * (i + 0.5f);

            // 加一点“乱”：沿墙方向和垂直方向都打散一点
            if (positionJitterAlong > 0f)
            {
                offsetAlong += Random.Range(-positionJitterAlong, positionJitterAlong);
            }

            float offsetPerp = 0f;
            if (positionJitterPerp > 0f)
            {
                offsetPerp = Random.Range(-positionJitterPerp, positionJitterPerp);
            }

            Vector3 flatPos = center + dir * offsetAlong + perp * offsetPerp;

            // 基础旋转再加一点 Y 轴随机旋转
            Quaternion rot = baseRotation;
            if (rotationJitterY > 0f)
            {
                float jitterY = Random.Range(-rotationJitterY, rotationJitterY);
                rot = rot * Quaternion.Euler(0f, jitterY, 0f);
            }

            SpawnTreeOnGround(flatPos, rot);
        }
    }

    /// <summary>
    /// 在给定平面位置生成一棵树，并自动贴合地面高度 + 最小距离检查。
    /// </summary>
    private void SpawnTreeOnGround(Vector3 flatPosition, Quaternion rotation)
    {
        GameObject prefab = GetRandomWallPrefab();
        if (prefab == null)
        {
            Debug.LogWarning("MazeBuilder: wallPrefabs 为空，无法生成树。");
            return;
        }

        // 先贴合到地面
        Vector3 groundPos = SnapToGround(flatPosition);
        Vector3 finalPos = groundPos + Vector3.up * groundOffset;

        // 检查与已有树的最小水平距离（XZ）
        if (minTreeDistance > 0f && placedTreePositions.Count > 0)
        {
            float minSqr = minTreeDistance * minTreeDistance;
            foreach (var p in placedTreePositions)
            {
                float dx = finalPos.x - p.x;
                float dz = finalPos.z - p.z;
                float sqr = dx * dx + dz * dz;
                if (sqr < minSqr)
                {
                    // 太近了，跳过这一棵
                    return;
                }
            }
        }

        GameObject tree = Instantiate(prefab, finalPos, rotation, transform);

        if (scaleWalls)
        {
            tree.transform.localScale = new Vector3(
                cellSize,
                wallHeight,
                wallThickness
            );
        }

        spawnedObjects.Add(tree);
        placedTreePositions.Add(finalPos);
    }

    /// <summary>
    /// 从 wallPrefabs 中随机挑一个
    /// </summary>
    private GameObject GetRandomWallPrefab()
    {
        if (wallPrefabs == null || wallPrefabs.Length == 0)
            return null;

        int idx = Random.Range(0, wallPrefabs.Length);
        return wallPrefabs[idx];
    }

    /// <summary>
    /// 使用 RaycastAll 贴地：忽略自身子物体，只选真正地面，避免树堆叠在树上。
    /// </summary>
    private Vector3 SnapToGround(Vector3 flatPosition)
    {
        Vector3 origin = flatPosition + Vector3.up * raycastHeight;
        Ray ray = new Ray(origin, Vector3.down);

        // 类似 MazeTreePlacer：用 RaycastAll，并过滤掉本对象的子物体:contentReference[oaicite:1]{index=1}
        RaycastHit[] hits = Physics.RaycastAll(ray, raycastHeight * 2f, groundLayer, QueryTriggerInteraction.Ignore);

        if (hits != null && hits.Length > 0)
        {
            float lowestY = float.MaxValue;
            bool foundGround = false;

            foreach (var hit in hits)
            {
                // 排除已经生成的树（都是 MazeBuilder 的子物体）
                if (hit.collider.transform.IsChildOf(transform))
                    continue;

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

        // 没打到地面，就保持原高度（y=0）
        return flatPosition;
    }

    public void ClearMaze()
    {
        foreach (var obj in spawnedObjects)
        {
            if (obj != null)
            {
                Destroy(obj);
            }
        }
        spawnedObjects.Clear();
        placedTreePositions.Clear();
        currentData = null;
    }

    public Vector3 GetWorldPosOfCell(MazeCell cell)
    {
        return CellToWorld(cell);
    }

    private Vector3 CellToWorld(MazeCell cell)
    {
        float originOffsetX = -(currentData.width - 1) * cellSize * 0.5f;
        float originOffsetZ = -(currentData.height - 1) * cellSize * 0.5f;

        return new Vector3(
            originOffsetX + cell.x * cellSize,
            0f,
            originOffsetZ + cell.y * cellSize
        );
    }
}
