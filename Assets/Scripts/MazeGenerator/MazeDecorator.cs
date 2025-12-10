using System.Collections.Generic;
using UnityEngine;

public class MazeDecorator : MonoBehaviour
{
    [Header("Refs")]
    [Tooltip("用于计算迷宫世界坐标（CellToWorld）")]
    public MazeBuilder mazeBuilder;

    // [Header("Path Decorations")]
    // public GameObject[] pathProps;
    // public int propsPerCell = 0;
    // public float propJitter = 0.5f;
    [Header("Path Decorations")]
    public GameObject[] pathProps;

    [Tooltip("每个 cell 最多生成多少个装饰，实际数量会在 0~propsPerCell 之间随机")]
    [Min(0)]
    public int propsPerCell = 2;

    [Tooltip("装饰物散布区域占 cell 的比例，1 = 整个 cell，0.5 = 只在中间一半范围内")]
    [Range(0.1f, 1.0f)]
    public float pathDecorAreaFactor = 0.8f;

    [Tooltip("装饰物的额外小幅抖动（米），用于打破完全规则的格子分布")]
    public float propJitter = 0.2f;

    [Tooltip("是否随机 Y 轴旋转地面装饰")]
    public bool randomPathPropRotationY = true;


    [Header("Outer Border Settings")]
    public bool generateOuterBorder = true;
    [Tooltip("外围层与层之间的间距缩放，比如 0.3 表示每层只用 30% 的 cellSize 做间隔")]
    [Range(0.1f, 1.0f)]
    public float outerLayerSpacingFactor = 0.4f;


    [Tooltip("在原本外圈之外再多生成几层（1 = 再加一圈，总共两圈）")]
    [Min(0)]
    public int extraBorderLayers = 2;

    [Tooltip("最内圈的基础密度（每条边棵数）")]
    public int borderTreesPerSide = 20;

    [Tooltip("每往外一层，密度增加的比例，例如 0.4 = 每往外一层 +40% 树")]
    public float borderLayerDensityGrowth = 0.4f;

    [Tooltip("最内圈的基础抖动")]
    public float borderJitter = 0.6f;

    [Tooltip("每往外一层，抖动增加的比例，例如 0.5 = +50% 抖动")]
    public float borderLayerJitterGrowth = 0.5f;

    [Tooltip("外围树的垂直偏移，用来微调离地高度")]
    public float borderGroundOffset = 0.0f;

    [Tooltip("从上往下射线检测的高度")]
    public float borderRaycastHeight = 10f;

    [Tooltip("地面 Layer（建议勾 Terrain / Ground 等）")]
    public LayerMask borderGroundLayer;

    [Header("Outer Border Appearance")]
    [Tooltip("外围树整体大小随机范围（基础缩放）")]
    public Vector2 borderTreeBaseScaleRange = new Vector2(1.1f, 1.4f);

    [Tooltip("每往外一层，额外增加的高度比例，例如 0.1 = 再高 10%")]
    public float borderLayerExtraHeightFactor = 0.12f;

    [Tooltip("外围树是否轻微倾斜，增加轮廓的“糊感”")]
    public bool borderAllowTilt = true;

    [Tooltip("外围树最大倾斜角度（度）")]
    public float borderMaxTiltAngle = 6f;

    // 记录所有装饰物，方便 ClearProps
    private readonly List<GameObject> spawnedProps = new List<GameObject>();

    // ================== 对外接口 ==================

    public void Decorate(MazeData data)
    {
        if (data == null || mazeBuilder == null)
        {
            Debug.LogWarning("MazeDecorator.Decorate: data 或 mazeBuilder 为空");
            return;
        }

        // 1. 路面小装饰（如果有）
        if (propsPerCell > 0 && pathProps != null && pathProps.Length > 0)
        {
            DecoratePaths(data);
        }

        // 2. 外围多层高树林
        if (generateOuterBorder && extraBorderLayers > 0)
        {
            GenerateOuterBorder(data);
        }
    }

    public void ClearProps()
    {
        foreach (var obj in spawnedProps)
        {
            if (obj != null)
                Destroy(obj);
        }
        spawnedProps.Clear();
    }

    // ================== 路面装饰（可按需改） ==================

private void DecoratePaths(MazeData data)
{
    // cellSize 用来控制散布范围
    float cellSize = mazeBuilder != null ? mazeBuilder.cellSize : 1f;
    float halfArea = cellSize * 0.5f * Mathf.Clamp01(pathDecorAreaFactor);

    for (int x = 0; x < data.width; x++)
    {
        for (int y = 0; y < data.height; y++)
        {
            MazeCell cell = data.cells[x, y];
            Vector3 cellCenter = mazeBuilder.GetWorldPosOfCell(cell);

            // 本 cell 实际生成多少个：0 ~ propsPerCell（包含上限）
            int countThisCell = (propsPerCell > 0)
                ? Random.Range(0, propsPerCell + 1)
                : 0;

            for (int i = 0; i < countThisCell; i++)
            {
                // 在 cell 的一个矩形区域内随机（越大越靠近 cell 边缘）
                float offsetX = Random.Range(-halfArea, halfArea);
                float offsetZ = Random.Range(-halfArea, halfArea);

                // 再叠加一点小 jitter，打破“格点感”
                offsetX += Random.Range(-propJitter, propJitter);
                offsetZ += Random.Range(-propJitter, propJitter);

                Vector3 flatPos = cellCenter + new Vector3(offsetX, 0f, offsetZ);
                Vector3 finalPos = SnapToGroundForBorder(flatPos);

                GameObject prefab = GetRandomPathProp();
                if (prefab == null)
                    continue;

                Quaternion rot = Quaternion.identity;
                if (randomPathPropRotationY)
                {
                    float yRot = Random.Range(0f, 360f);
                    rot = Quaternion.Euler(0f, yRot, 0f);
                }

                GameObject inst = Instantiate(prefab, finalPos, rot, transform);
                spawnedProps.Add(inst);
            }
        }
    }
}


    private GameObject GetRandomPathProp()
    {
        if (pathProps == null || pathProps.Length == 0)
            return null;
        int idx = Random.Range(0, pathProps.Length);
        return pathProps[idx];
    }

    // ================== 外围多层树林 ==================

    private void GenerateOuterBorder(MazeData data)
    {
        if (mazeBuilder.wallPrefabs == null || mazeBuilder.wallPrefabs.Length == 0)
        {
            Debug.LogWarning("MazeDecorator.GenerateOuterBorder: mazeBuilder.wallPrefabs 为空，无法生成外围树。");
            return;
        }

        // 1. 算世界坐标范围
        MazeCell blCell = data.GetCell(0, 0);
        MazeCell trCell = data.GetCell(data.width - 1, data.height - 1);

        Vector3 blWorld = mazeBuilder.GetWorldPosOfCell(blCell);
        Vector3 trWorld = mazeBuilder.GetWorldPosOfCell(trCell);

        float minX = Mathf.Min(blWorld.x, trWorld.x);
        float maxX = Mathf.Max(blWorld.x, trWorld.x);
        float minZ = Mathf.Min(blWorld.z, trWorld.z);
        float maxZ = Mathf.Max(blWorld.z, trWorld.z);

        float cellSize = mazeBuilder.cellSize;

        // 2. 一圈圈往外扩
        for (int layer = 1; layer <= extraBorderLayers; layer++)
        {
            float offset = cellSize * layer * outerLayerSpacingFactor;

            float layerMinX = minX - offset;
            float layerMaxX = maxX + offset;
            float layerMinZ = minZ - offset;
            float layerMaxZ = maxZ + offset;

            // 每一圈的“糊度”：越外圈越密 & 越抖
            float densityFactor = 1f + borderLayerDensityGrowth * (layer - 1);
            int treesThisLayer = Mathf.Max(1,
                Mathf.RoundToInt(borderTreesPerSide * densityFactor));

            float jitterFactor = 1f + borderLayerJitterGrowth * (layer - 1);
            float jitterThisLayer = borderJitter * jitterFactor;

            // 下、上、左、右四条边
            SpawnTreesAlongLine(
                new Vector3(layerMinX, 0f, layerMinZ),
                new Vector3(layerMaxX, 0f, layerMinZ),
                treesThisLayer, layer, jitterThisLayer);

            SpawnTreesAlongLine(
                new Vector3(layerMinX, 0f, layerMaxZ),
                new Vector3(layerMaxX, 0f, layerMaxZ),
                treesThisLayer, layer, jitterThisLayer);

            SpawnTreesAlongLine(
                new Vector3(layerMinX, 0f, layerMinZ),
                new Vector3(layerMinX, 0f, layerMaxZ),
                treesThisLayer, layer, jitterThisLayer);

            SpawnTreesAlongLine(
                new Vector3(layerMaxX, 0f, layerMinZ),
                new Vector3(layerMaxX, 0f, layerMaxZ),
                treesThisLayer, layer, jitterThisLayer);
        }
    }

    private void SpawnTreesAlongLine(Vector3 start, Vector3 end, int count, int layerIndex, float jitterThisLayer)
    {
        if (count <= 0) return;

        for (int i = 0; i < count; i++)
        {
            float t = (i + 0.5f) / count;
            Vector3 pos = Vector3.Lerp(start, end, t);

            Vector3 dir = (end - start);
            dir.y = 0f;
            Vector3 along = dir.normalized;
            Vector3 perp = new Vector3(-along.z, 0f, along.x);

            float jitterAlong = Random.Range(-jitterThisLayer, jitterThisLayer);
            float jitterPerp = Random.Range(-jitterThisLayer, jitterThisLayer);

            Vector3 flatPos = pos + along * jitterAlong + perp * jitterPerp;

            Vector3 groundPos = SnapToGroundForBorder(flatPos) + Vector3.up * borderGroundOffset;

            GameObject treePrefab = GetRandomBorderTreePrefab();
            if (treePrefab == null) return;

            GameObject inst = Instantiate(treePrefab, groundPos, RandomYRotation(), transform);

            // ====== 关键：外围树比里面更高 & 外层更高 ======
            float baseScale = Random.Range(borderTreeBaseScaleRange.x, borderTreeBaseScaleRange.y);
            float heightBoost = 1f + borderLayerExtraHeightFactor * (layerIndex - 1);

            Vector3 s = inst.transform.localScale;
            // X/Z 做整体缩放，Y 再乘一个额外高度系数 -> 看起来更高更“墙”
            s.x *= baseScale;
            s.z *= baseScale;
            s.y *= baseScale * heightBoost;
            inst.transform.localScale = s;

            // 轻微倾斜，轮廓更“糊”
            if (borderAllowTilt)
            {
                Vector3 e = inst.transform.eulerAngles;
                float tilt = borderMaxTiltAngle;
                e.x += Random.Range(-tilt, tilt);
                e.z += Random.Range(-tilt, tilt);
                inst.transform.eulerAngles = e;
            }

            spawnedProps.Add(inst);
        }
    }

    private GameObject GetRandomBorderTreePrefab()
    {
        if (mazeBuilder == null || mazeBuilder.wallPrefabs == null || mazeBuilder.wallPrefabs.Length == 0)
            return null;
        int idx = Random.Range(0, mazeBuilder.wallPrefabs.Length);
        return mazeBuilder.wallPrefabs[idx];
    }

    private Quaternion RandomYRotation()
    {
        float y = Random.Range(0f, 360f);
        return Quaternion.Euler(0f, y, 0f);
    }

    private Vector3 SnapToGroundForBorder(Vector3 flatPosition)
    {
        Vector3 origin = flatPosition + Vector3.up * borderRaycastHeight;
        Ray ray = new Ray(origin, Vector3.down);

        if (Physics.Raycast(ray, out RaycastHit hit, borderRaycastHeight * 2f, borderGroundLayer,
                QueryTriggerInteraction.Ignore))
        {
            return new Vector3(flatPosition.x, hit.point.y, flatPosition.z);
        }

        return flatPosition;
    }
}
