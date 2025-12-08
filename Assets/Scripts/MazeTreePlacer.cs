using UnityEngine;

/// <summary>
/// 根据迷宫遮罩纹理在场景中实例化树木作为障碍物。
/// 假设遮罩是黑白图：黑色=障碍，白色=可通行。图像中心对应场景原点。
/// 支持多个树预制体，生成时随机选择。
/// 支持地形高度自适应，使用 Raycast 检测地面，确保树木贴合地形。
/// 将本脚本挂到一个空物体上，设置遮罩、树预制体数组和半径后运行。
/// </summary>
public class MazeTreePlacer : MonoBehaviour
{
    [Header("输入资源")]
    [SerializeField] private Texture2D mazeMask;          // 迷宫黑白纹理（黑=障碍）
    [SerializeField] private GameObject[] treePrefabs;     // 树预制体数组，随机选择

    [Header("生成范围")]
    [SerializeField] private float radius = 20f;           // 圆形迷宫在场景中的半径（XZ 平面）
    [SerializeField] private float yOffset = 0f;           // 树的高度偏移（仅在关闭地形检测时使用）
    [SerializeField] private int pixelStep = 3;            // 取样步长：越小越精细，性能开销越大
    [SerializeField, Range(0f, 1f)] private float wallThreshold = 0.5f; // 亮度阈值，低于该值认为是障碍
    [SerializeField] private float minDistance = 1.5f;     // 树之间的最小距离（世界坐标），避免堆叠
    [SerializeField] private int maxTrees = 0;             // 0 表示不限制数量

    [Header("地形检测")]
    [SerializeField] private bool useGroundDetection = true;    // 是否启用地形高度检测
    [SerializeField] private float raycastHeight = 100f;        // Raycast 起始高度（相对于 transform.position.y）
    [SerializeField] private float raycastDistance = 200f;      // Raycast 最大检测距离
    [SerializeField] private LayerMask groundLayer = ~0;        // 地面层遮罩（默认所有层）

    [Header("随机外观")]
    [SerializeField] private Vector2 randomScaleRange = new Vector2(0.9f, 1.3f);
    [SerializeField] private bool randomRotation = true;

    [Header("调试")]
    [SerializeField] private bool clearOnGenerate = true;  // 生成前是否清除已有子物体
    [SerializeField] private bool showDebugInfo = false;   // 显示详细调试信息

    /// <summary>在播放或手动调用时生成树障碍。</summary>
    [ContextMenu("Generate Trees From Mask")]
    public void Generate()
    {
        if (mazeMask == null || treePrefabs == null || treePrefabs.Length == 0)
        {
            Debug.LogWarning("MazeTreePlacer: 请先设置 mazeMask 和 treePrefabs（至少一个预制体）。");
            return;
        }

        if (clearOnGenerate)
        {
            ClearChildren();
        }

        int placed = 0;
        int skipped = 0;
        int raycastMissed = 0;  // 射线未击中地面的次数
        int tooCloseCount = 0;  // 距离太近被跳过的次数
        float halfW = mazeMask.width * 0.5f;
        float halfH = mazeMask.height * 0.5f;

        // 记录已生成树木的位置，用于距离检查
        System.Collections.Generic.List<Vector3> placedPositions = new System.Collections.Generic.List<Vector3>();

        for (int y = 0; y < mazeMask.height; y += Mathf.Max(1, pixelStep))
        {
            for (int x = 0; x < mazeMask.width; x += Mathf.Max(1, pixelStep))
            {
                Color c = mazeMask.GetPixel(x, y);
                float luminance = c.grayscale;
                bool isWall = luminance <= wallThreshold;
                if (!isWall) continue;

                // 将纹理坐标映射到单位圆，再映射到世界坐标
                float nx = (x - halfW) / halfW; // -1..1
                float nz = (y - halfH) / halfH; // -1..1
                Vector2 uv = new Vector2(nx, nz);
                if (uv.sqrMagnitude > 1f) continue; // 只在圆内放置

                // 先计算 XZ 平面位置
                Vector3 xzPos = new Vector3(uv.x * radius, 0f, uv.y * radius) + transform.position;

                // 地形高度检测
                Vector3 worldPos;
                if (useGroundDetection)
                {
                    // 从高处向下发射射线
                    Vector3 rayOrigin = new Vector3(xzPos.x, transform.position.y + raycastHeight, xzPos.z);

                    // 使用 RaycastAll 获取所有击中点，找最低点（真正的地面）
                    RaycastHit[] hits = Physics.RaycastAll(rayOrigin, Vector3.down, raycastDistance, groundLayer);

                    if (hits.Length > 0)
                    {
                        // 找到 Y 值最小的击中点（最低的地面）
                        float lowestY = float.MaxValue;
                        bool foundGround = false;

                        foreach (RaycastHit hit in hits)
                        {
                            // 排除已生成的树木（检查是否是 transform 的子物体）
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
                            worldPos = new Vector3(xzPos.x, lowestY, xzPos.z);
                            if (showDebugInfo)
                                Debug.Log($"找到地面: XZ({xzPos.x:F2}, {xzPos.z:F2}) -> Y={lowestY:F2}");
                        }
                        else
                        {
                            // 所有击中点都是树木，跳过该位置
                            skipped++;
                            raycastMissed++;
                            if (showDebugInfo)
                                Debug.LogWarning($"位置 ({xzPos.x:F2}, {xzPos.z:F2}) 只击中树木，跳过");
                            continue;
                        }
                    }
                    else
                    {
                        // 未击中任何物体，跳过该位置
                        skipped++;
                        raycastMissed++;
                        if (showDebugInfo)
                            Debug.LogWarning($"位置 ({xzPos.x:F2}, {xzPos.z:F2}) 射线未击中任何物体");
                        continue;
                    }
                }
                else
                {
                    // 不使用地形检测，使用固定 yOffset（保留原有功能）
                    worldPos = new Vector3(xzPos.x, transform.position.y + yOffset, xzPos.z);
                }

                // 检查与已生成树木的最小距离（只检查 XZ 平面的水平距离，忽略高度）
                bool tooClose = false;
                if (minDistance > 0f)
                {
                    foreach (Vector3 existingPos in placedPositions)
                    {
                        // 只比较 XZ 平面距离，避免树木垂直堆叠
                        float dx = worldPos.x - existingPos.x;
                        float dz = worldPos.z - existingPos.z;
                        float sqrDistXZ = dx * dx + dz * dz;

                        if (sqrDistXZ < minDistance * minDistance)
                        {
                            tooClose = true;
                            tooCloseCount++;
                            if (showDebugInfo)
                                Debug.Log($"位置 ({worldPos.x:F2}, {worldPos.z:F2}) 距离已有树木太近，跳过");
                            break;
                        }
                    }
                }
                if (tooClose) continue;

                Quaternion rot = randomRotation ? Quaternion.Euler(0f, Random.Range(0f, 360f), 0f) : Quaternion.identity;

                // 随机选择一个树预制体
                GameObject selectedPrefab = treePrefabs[Random.Range(0, treePrefabs.Length)];
                GameObject tree = Instantiate(selectedPrefab, worldPos, rot, transform);

                float scale = Random.Range(randomScaleRange.x, randomScaleRange.y);
                tree.transform.localScale = Vector3.one * scale;

                placedPositions.Add(worldPos);
                placed++;
                if (maxTrees > 0 && placed >= maxTrees)
                {
                    Debug.Log($"MazeTreePlacer: 已达到 maxTrees={maxTrees}，提前结束生成。");
                    Debug.Log($"MazeTreePlacer: 实例化树数量 = {placed}");
                    Debug.Log($"  - 射线未击中: {raycastMissed} 个位置");
                    Debug.Log($"  - 距离太近: {tooCloseCount} 个位置");
                    return;
                }
            }
        }

        Debug.Log($"MazeTreePlacer: 实例化树数量 = {placed}，总跳过 {skipped} 个位置");
        Debug.Log($"  - 射线未击中地面: {raycastMissed} 个位置");
        Debug.Log($"  - 距离太近被跳过: {tooCloseCount} 个位置");
    }

    private void ClearChildren()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            #if UNITY_EDITOR
            if (!Application.isPlaying)
                DestroyImmediate(transform.GetChild(i).gameObject);
            else
                Destroy(transform.GetChild(i).gameObject);
            #else
            Destroy(transform.GetChild(i).gameObject);
            #endif
        }
    }
}

