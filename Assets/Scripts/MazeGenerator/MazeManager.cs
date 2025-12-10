using UnityEngine;

public class MazeManager : MonoBehaviour
{
    [Header("Refs")]
    public MazeBuilder mazeBuilder;
    public Transform player;

    [Header("Global Maze Size Settings (当不用序列时生效)")]
    public int initialWidth = 10;
    public int initialHeight = 10;
    public bool randomSizeEachLevel = false;
    public int minSize = 8;
    public int maxSize = 20;

    [System.Serializable]
    public class MazeSequenceEntry
    {
        [Tooltip("这一关要用的迷宫类型")]
        public MazeType mazeType = MazeType.DFS;

        [Tooltip("这一关迷宫宽度")]
        public int width = 10;

        [Tooltip("这一关迷宫高度")]
        public int height = 10;
    }
    [Header("Decoration")]
    public MazeDecorator mazeDecorator;

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
        currentMazeData = MazeGenerator.Generate(config);

        if (currentMazeData == null)
        {
            Debug.LogError("MazeManager: Failed to generate maze data.");
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
            Vector3 startPos = mazeBuilder.GetWorldPosOfCell(currentMazeData.startCell) + Vector3.up * 10f;
            player.position = startPos;
        }

        currentIndex++;
    }
}
