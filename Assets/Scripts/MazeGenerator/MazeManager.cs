using UnityEngine;

public class MazeManager : MonoBehaviour
{
    [Header("Refs")]
    public MazeBuilder mazeBuilder;
    public Transform player;

    [Header("Maze Settings")]
    public int initialWidth = 10;
    public int initialHeight = 10;
    public bool randomSizeEachLevel = false;
    public int minSize = 8;
    public int maxSize = 20;

    private int currentIndex = 0;
    private MazeData currentMazeData;

    private void Start()
    {
        GenerateNewMaze();
    }

    public void GenerateNewMaze()
    {
        // 1. 清理旧迷宫
        mazeBuilder.ClearMaze();

        // 2. 创建本关的 MazeConfig
        MazeConfig config = new MazeConfig();
        config.type = MazeType.Grid;

        if (randomSizeEachLevel)
        {
            config.width = Random.Range(minSize, maxSize + 1);
            config.height = Random.Range(minSize, maxSize + 1);
        }
        else
        {
            config.width = initialWidth;
            config.height = initialHeight;
        }

        // 3. 生成迷宫数据
        currentMazeData = MazeGeneratorDFS.Generate(config);

        // 4. 实例化到场景
        mazeBuilder.Build(currentMazeData);

        // 5. 把玩家放到起点
        if (player != null && currentMazeData.startCell != null)
        {
            Vector3 startPos = mazeBuilder.GetWorldPosOfCell(currentMazeData.startCell) + Vector3.up * 1f;
            player.position = startPos;
        }

        currentIndex++;
    }
}
