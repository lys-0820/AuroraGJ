#if UNITY_EDITOR
using UnityEditor;
#endif

using UnityEngine;
using System.Collections.Generic;
public class MazeManager : MonoBehaviour
{
    [Header("Refs")]
    public MazeBuilder mazeBuilder;
    public Transform player;
    [Header("Deer & Path")]          // 鹿相关引用
    public DeerController deer;
    public RuntimeMainPath runtimeMainPath;
    [Header("Global Maze Size Settings (当不用序列时生效)")]
    public int initialWidth = 10;
    public int initialHeight = 10;
    public bool randomSizeEachLevel = false;
    public int minSize = 8;
    public int maxSize = 20;
    [Header("Tree Variety")]
    [Tooltip("默认使用前多少种树 prefab（0 或负数表示使用全部）")]
    public int defaultTreeVariantCount = 0;
    [System.Serializable]
    public class MazeSequenceEntry
    {
        [Tooltip("这一关要用的迷宫类型")]
        public MazeType mazeType = MazeType.DFS;

        [Tooltip("这一关迷宫宽度")]
        public int width = 10;

        [Tooltip("这一关迷宫高度")]
        public int height = 10;

        [Tooltip("这一关使用前多少种树 prefab（0 或负数表示使用默认或全部）")]
        public int treeVariantCount = 0;
        [Header("Deer Path Override (可选覆盖全局设置，<=0 表示用全局)")]
        public float deerFirstPointMinDistance = -1f;
        public float deerPointMinDistance = -1f;
    }
    [Header("Decoration")]
    public MazeDecorator mazeDecorator;
    [Header("Deer Path Settings (每关引导难度)")]
    [Tooltip("第一个鹿路径点离玩家起点的最小距离（世界坐标，单位：米）")]
    public float deerFirstPointMinDistance = 15f;

    [Tooltip("相邻两个鹿路径点之间的最小距离（世界坐标，单位：米）。越大越稀疏，难度越高")]
    public float deerPointMinDistance = 20f;

    [Header("Maze Type & Sequence")]
    [Tooltip("是否按关卡顺序使用不同迷宫类型 + 难度")]
    public bool useMazeSequence = false;

    [Tooltip("当不使用序列时，默认使用的迷宫类型")]
    public MazeType defaultMazeType = MazeType.DFS;

    [Tooltip("关卡顺序使用的迷宫配置数组（类型 + 宽高）")]
    public MazeSequenceEntry[] mazeSequence;

    [Header("Debug")]
    [Tooltip("勾上后可以在运行时通过按键重新生成迷宫")]
    public bool enableDebugRegenerate = true;

    [Tooltip("运行时重新生成迷宫的按键")]
    public KeyCode debugRegenerateKey = KeyCode.R;
    [Header("Gizmo Preview")]
    [Tooltip("在 Scene 视图中显示迷宫大小预览")]
    public bool showMazeSizeGizmos = true;

    [Tooltip("当前迷宫（或当前 index）用什么颜色画")]
    public Color currentMazeColor = new Color(0.2f, 1f, 0.2f, 1f);

    [Tooltip("其他序列中的迷宫用什么颜色画")]
    public Color otherMazeColor = new Color(0.2f, 0.8f, 1f, 1f);

    [Tooltip("多个迷宫并排预览时的间距（世界坐标）")]
    public float gizmoSpacing = 2f;

    [Tooltip("线框在地面上方稍微抬高一点，避免和地板重合看不清")]
    public float gizmoYOffset = 0.05f;


    private int currentIndex = 0;        // 当前关卡索引，从 0 开始
    private MazeData currentMazeData;

    private void Start()
    {
        GenerateNewMaze();
    }

    private void Update()
    {
        if (!enableDebugRegenerate)
            return;

        if (Input.GetKeyDown(debugRegenerateKey))
        {
            Debug.Log($"[MazeManager] Debug regenerate key pressed: {debugRegenerateKey}");
            GenerateNewMaze();
        }
    }

    /// <summary>
    /// 供 MazeExit 调用：玩家到达终点后生成新迷宫。
    /// </summary>
    public void GenerateNewMaze()
    {
        if (mazeBuilder == null)
        {
            Debug.LogError("MazeManager: MazeBuilder reference is missing!");
            return;
        }

        // 1. 清理旧迷宫
        mazeBuilder.ClearMaze();
        if (mazeDecorator != null)
            mazeDecorator.ClearProps();
        TrailParticles.ClearAllTrails();
        currentMazeData = null;

        // 2. 配置本关迷宫参数
        MazeConfig config = new MazeConfig();

        if (useMazeSequence && mazeSequence != null && mazeSequence.Length > 0)
        {
            // 使用关卡序列：类型 + 尺寸都来自序列
            int idx = currentIndex % mazeSequence.Length;
            MazeSequenceEntry entry = mazeSequence[idx];

            // 避免填 0 或负数导致崩
            int w = Mathf.Max(4, entry.width);
            int h = Mathf.Max(4, entry.height);

            config.width = w;
            config.height = h;
            config.type = entry.mazeType;
            // 设置树种类数量
            int n = entry.treeVariantCount > 0 ? entry.treeVariantCount : defaultTreeVariantCount;
            mazeBuilder.maxTreeTypesToUse = n;
        }
        else
        {
            // 不用序列时，保持原来的逻辑
            if (randomSizeEachLevel)
            {
                int w = Random.Range(minSize, maxSize + 1);
                int h = Random.Range(minSize, maxSize + 1);
                config.width = w;
                config.height = h;
            }
            else
            {
                config.width = initialWidth;
                config.height = initialHeight;
            }

            config.type = defaultMazeType;
        }

        // 3. 生成迷宫数据（通过统一入口 MazeGenerator）
        const int maxAttempts = 5;
        int attempt = 0;
        do
        {
            currentMazeData = MazeGenerator.Generate(config);
            attempt++;

            if (currentMazeData == null)
            {
                Debug.LogError("MazeManager: Failed to generate maze data.");
                return;
            }

            var debugPath = MazeSolver.FindPath(currentMazeData);
            if (debugPath != null)
            {
                break;  // OK，这一关有路
            }

            Debug.LogWarning($"[MazeManager] 生成的迷宫没有路径，第 {attempt} 次重试。");
} 
while (attempt < maxAttempts);

if (MazeSolver.FindPath(currentMazeData) == null)
{
    Debug.LogError("[MazeManager] 多次尝试仍未生成可解迷宫，请检查生成逻辑。");
    return;
}


        // 4. 实例化到场景
        mazeBuilder.Build(currentMazeData);
        // 4.5 路面装饰
        if (mazeDecorator != null)
        {
            mazeDecorator.Decorate(currentMazeData);
        }
        // 5. 把玩家放到起点
        if (player != null && currentMazeData.startCell != null)
        {
            Vector3 startPos = GetSafePlayerSpawnPosition();
            player.position = startPos;

            // 如果有刚体 / CharacterController，建议也把速度清一下
            var rb = player.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
}

        // 6. 生成给鹿用的主路线，并通知鹿（新加的逻辑）
        SetupDeerPathForCurrentMaze();

        currentIndex++;
    }
    /// <summary>
/// 基于当前迷宫数据，自动生成一条“鹿主路线”
/// - 不在起点附近放第一个点
/// - 相邻点之间至少 deerPointMinDistance
/// - 可以被 MazeSequenceEntry 覆盖 per-level
/// </summary>
private void SetupDeerPathForCurrentMaze()
{
    if (runtimeMainPath == null || currentMazeData == null || mazeBuilder == null)
        return;

    // 0. 计算本关实际使用的两个参数（如果有序列就覆盖）
    float firstPointMinDist = deerFirstPointMinDistance;
    float segmentMinDist = deerPointMinDistance;

    if (useMazeSequence && mazeSequence != null && mazeSequence.Length > 0)
    {
        int idx = currentIndex % mazeSequence.Length;
        MazeSequenceEntry entry = mazeSequence[idx];

        if (entry.deerFirstPointMinDistance > 0f)
            firstPointMinDist = entry.deerFirstPointMinDistance;

        if (entry.deerPointMinDistance > 0f)
            segmentMinDist = entry.deerPointMinDistance;
    }

    // 1. 解迷宫：先拿到完整 path，再做一次“转弯抽稀”
    var fullPath = MazeSolver.FindPath(currentMazeData);
    if (fullPath == null || fullPath.Count == 0)
    {
        Debug.LogWarning("MazeManager: MazeSolver 没有找到从起点到终点的路径。");
        return;
    }

    var cornerPath = MazeSolver.ExtractCorners(fullPath);

    // 2. 按“距离规则”进一步筛选：
    //   - 第一个点必须离玩家起点 >= firstPointMinDist
    //   - 相邻点之间 >= segmentMinDist
    var deerCells = new System.Collections.Generic.List<MazeCell>();

    // 玩家起点的世界坐标
    Vector3 playerStartPos = mazeBuilder.GetWorldPosOfCell(currentMazeData.startCell);

    bool hasLast = false;
    Vector3 lastPos = Vector3.zero;

    foreach (var cell in cornerPath)
    {
        // 明确跳过 startCell，自然保证“第一个点不要放在起点”
        if (cell == currentMazeData.startCell)
            continue;

        Vector3 wpPos = mazeBuilder.GetWorldPosOfCell(cell);

        // 2.1 第一个点：先要求离玩家起点足够远
        if (!hasLast)
        {
            float distToPlayer = Vector3.Distance(wpPos, playerStartPos);
            if (distToPlayer < firstPointMinDist)
            {
                // 还在起点附近，继续往后找
                continue;
            }

            deerCells.Add(cell);
            hasLast = true;
            lastPos = wpPos;
        }
        else
        {
            // 2.2 后续点：和上一个鹿点之间必须 >= segmentMinDist
            float distToLast = Vector3.Distance(wpPos, lastPos);
            if (distToLast < segmentMinDist)
            {
                // 太近，跳过
                continue;
            }

            deerCells.Add(cell);
            lastPos = wpPos;
        }
    }

    // 3. 确保终点一定在列表中（不满足距离也要强行加上）
    var endCell = currentMazeData.endCell;
    if (endCell != null)
    {
        if (deerCells.Count == 0 || deerCells[deerCells.Count - 1] != endCell)
        {
            deerCells.Add(endCell);
        }
    }

    // 4. 用最终 deerCells 构建 RuntimeMainPath
    runtimeMainPath.BuildFromCellPath(deerCells, mazeBuilder);

    // 5. 通知鹿换路径
    if (deer != null)
    {
        deer.ResetWithNewPath(runtimeMainPath);
    }
}

// 获取一个安全的玩家出生位置，避免埋进地里或上天
private Vector3 GetSafePlayerSpawnPosition()
{
    if (currentMazeData == null || currentMazeData.startCell == null || mazeBuilder == null)
    {
        return player != null ? player.position : Vector3.zero;
    }

    // 1. 先拿到格子中心的世界坐标（XZ）
    Vector3 flatPos = mazeBuilder.GetWorldPosOfCell(currentMazeData.startCell);

    // 2. 使用和 MazeBuilder 类似的贴地逻辑：从上往下 RaycastAll
    float rayHeight = mazeBuilder.raycastHeight;      // 直接用 MazeBuilder 里的高度
    LayerMask groundMask = mazeBuilder.groundLayer;   // 用同一套地面层

    Vector3 origin = flatPos + Vector3.up * rayHeight;
    Ray ray = new Ray(origin, Vector3.down);

    RaycastHit[] hits = Physics.RaycastAll(
        ray,
        rayHeight * 2f,
        groundMask,
        QueryTriggerInteraction.Ignore
    );

    float groundY = flatPos.y;
    bool foundGround = false;

    if (hits != null && hits.Length > 0)
    {
        foreach (var hit in hits)
        {
            // **关键：忽略 MazeBuilder 生成的树**
            if (hit.collider.transform.IsChildOf(mazeBuilder.transform))
                continue;

            if (!foundGround || hit.point.y < groundY)
            {
                groundY = hit.point.y;
                foundGround = true;
            }
        }
    }

    Vector3 basePos = new Vector3(flatPos.x, foundGround ? groundY : flatPos.y, flatPos.z);

    // 3. 给玩家留一点高度，避免埋进地里（视自己角色高度调整）
    float playerHeightOffset = 1.0f;
    basePos += Vector3.up * playerHeightOffset;

    return basePos;
}
    // 迷宫大小预览
    private void OnDrawGizmosSelected()
    {
        if (!showMazeSizeGizmos || mazeBuilder == null)
            return;

        float cellSize = mazeBuilder.cellSize > 0f ? mazeBuilder.cellSize : 1f;

        // 收集所有要画的迷宫尺寸（全局 + sequence）
        var mazeSizes = new System.Collections.Generic.List<(string label, int width, int height, bool isCurrent)>();

        // 1）如果有 sequence，就按 sequence 预览
        if (useMazeSequence && mazeSequence != null && mazeSequence.Length > 0)
        {
            for (int i = 0; i < mazeSequence.Length; i++)
            {
                var entry = mazeSequence[i];
                if (entry == null)
                    continue;

                string label = string.IsNullOrEmpty(entry.mazeType.ToString())
                    ? $"Level {i}"
                    : $"{i}: {entry.mazeType.ToString()}";

                bool isCurrent = (i == (currentIndex % mazeSequence.Length));

                mazeSizes.Add((label, entry.width, entry.height, isCurrent));
            }
        }
        else
        {
            // 2）没用 sequence，就只画一个全局尺寸
            mazeSizes.Add(("Global", initialWidth, initialHeight, true));
        }

        if (mazeSizes.Count == 0)
            return;

        // 计算最大宽度，用来确定排布间距
        float maxWorldWidth = 0f;
        for (int i = 0; i < mazeSizes.Count; i++)
        {
            var ms = mazeSizes[i];
            float w = ms.width * cellSize;
            if (w > maxWorldWidth) maxWorldWidth = w;
        }

        // 以 MazeManager 的位置为中心，沿着 X 轴排开
        Vector3 origin = transform.position;
        float totalWidth = mazeSizes.Count * maxWorldWidth + (mazeSizes.Count - 1) * gizmoSpacing;
        float startOffsetX = -totalWidth * 0.5f + maxWorldWidth * 0.5f;

        for (int i = 0; i < mazeSizes.Count; i++)
        {
            var ms = mazeSizes[i];

            float worldWidth = ms.width * cellSize;
            float worldHeight = ms.height * cellSize;

            // 每一个线框的中心位置
            Vector3 center = origin
                             + transform.right * (startOffsetX + i * (maxWorldWidth + gizmoSpacing))
                             + Vector3.up * gizmoYOffset;

            Vector3 size = new Vector3(worldWidth, 0.01f, worldHeight);

            Gizmos.color = ms.isCurrent ? currentMazeColor : otherMazeColor;
            Gizmos.DrawWireCube(center, size);

#if UNITY_EDITOR
            // 在场景里显示文字：关卡名 + 宽高
            Handles.Label(
                center + Vector3.up * 0.3f,
                $"{ms.label}\n{ms.width} x {ms.height}"
            );
#endif
        }
    }

}
