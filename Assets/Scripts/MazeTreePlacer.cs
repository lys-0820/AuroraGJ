using UnityEngine;

/// <summary>
/// 根据迷宫遮罩纹理在场景中实例化树木作为障碍物。
/// 假设遮罩是黑白图：黑色=障碍，白色=可通行。图像中心对应场景原点。
/// 支持多个树预制体，生成时随机选择。
/// 将本脚本挂到一个空物体上，设置遮罩、树预制体数组和半径后运行。
/// </summary>
public class MazeTreePlacer : MonoBehaviour
{
    [Header("输入资源")]
    [SerializeField] private Texture2D mazeMask;          // 迷宫黑白纹理（黑=障碍）
    [SerializeField] private GameObject[] treePrefabs;     // 树预制体数组，随机选择

    [Header("生成范围")]
    [SerializeField] private float radius = 20f;           // 圆形迷宫在场景中的半径（XZ 平面）
    [SerializeField] private float yOffset = 0f;           // 树的高度偏移
    [SerializeField] private int pixelStep = 3;            // 取样步长：越小越精细，性能开销越大
    [SerializeField, Range(0f, 1f)] private float wallThreshold = 0.5f; // 亮度阈值，低于该值认为是障碍
    [SerializeField] private float minDistance = 1.5f;     // 树之间的最小距离（世界坐标），避免堆叠
    [SerializeField] private int maxTrees = 0;             // 0 表示不限制数量

    [Header("随机外观")]
    [SerializeField] private Vector2 randomScaleRange = new Vector2(0.9f, 1.3f);
    [SerializeField] private bool randomRotation = true;

    [Header("调试")]
    [SerializeField] private bool clearOnGenerate = true;  // 生成前是否清除已有子物体

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

                Vector3 worldPos = new Vector3(uv.x * radius, yOffset, uv.y * radius) + transform.position;

                // 检查与已生成树木的最小距离
                bool tooClose = false;
                if (minDistance > 0f)
                {
                    foreach (Vector3 existingPos in placedPositions)
                    {
                        float sqrDist = (worldPos - existingPos).sqrMagnitude;
                        if (sqrDist < minDistance * minDistance)
                        {
                            tooClose = true;
                            skipped++;
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
                    Debug.Log($"MazeTreePlacer: 实例化树数量 = {placed}，跳过 {skipped} 个距离过近的位置。");
                    return;
                }
            }
        }

        Debug.Log($"MazeTreePlacer: 实例化树数量 = {placed}，跳过 {skipped} 个距离过近的位置。");
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

