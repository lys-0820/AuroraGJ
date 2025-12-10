using System.Collections.Generic;
using UnityEngine;

public static class MazeGeneratorPrim
{
    public static MazeData Generate(MazeConfig config)
    {
        MazeData data = new MazeData(config.width, config.height);

        int width = config.width;
        int height = config.height;

        // 标记某个格子是否已经加入迷宫
        bool[,] inMaze = new bool[width, height];

        // frontier: 邻接迷宫树边界的 cell（还没加入）
        List<MazeCell> frontier = new List<MazeCell>();

        // 1. 随机选一个起点
        int startX = Random.Range(0, width);
        int startY = Random.Range(0, height);
        MazeCell startCell = data.GetCell(startX, startY);
        inMaze[startX, startY] = true;

        // 把起点的邻居加入 frontier
        AddFrontierNeighbors(data, startCell, inMaze, frontier);

        // 2. 不断从 frontier 里吸收新的 cell
        while (frontier.Count > 0)
        {
            int idx = Random.Range(0, frontier.Count);
            MazeCell cell = frontier[idx];
            frontier.RemoveAt(idx);

            // 找到它周围已经在迷宫里的邻居
            List<MazeCell> neighborsInMaze = GetNeighborsInMaze(data, cell, inMaze);
            if (neighborsInMaze.Count == 0)
            {
                // 理论上不会发生，但防御一下
                continue;
            }

            // 随机选一个邻居，把墙打通
            MazeCell neighbor = neighborsInMaze[Random.Range(0, neighborsInMaze.Count)];
            RemoveWallBetween(neighbor, cell);

            // 把该 cell 加入迷宫
            inMaze[cell.x, cell.y] = true;

            // 再把这个 cell 的邻居加入 frontier
            AddFrontierNeighbors(data, cell, inMaze, frontier);
        }

        data.startCell = startCell;
        data.endCell = FindFarthestCellByPathLength(data, startCell);

        return data;
    }

    /// <summary>
    /// 把 cell 周围还不在迷宫里的格子加入 frontier 列表
    /// </summary>
    private static void AddFrontierNeighbors(MazeData data, MazeCell cell, bool[,] inMaze, List<MazeCell> frontier)
    {
        int[][] dirs = new int[][]
        {
            new int[]{ 0, 1 },   // 上
            new int[]{ 1, 0 },   // 右
            new int[]{ 0, -1 },  // 下
            new int[]{ -1, 0 }   // 左
        };

        foreach (var d in dirs)
        {
            int nx = cell.x + d[0];
            int ny = cell.y + d[1];
            MazeCell n = data.GetCell(nx, ny);
            if (n == null) continue;

            if (!inMaze[nx, ny] && !frontier.Contains(n))
            {
                frontier.Add(n);
            }
        }
    }

    /// <summary>
    /// 返回某个 cell 周围已经在迷宫中的邻居
    /// </summary>
    private static List<MazeCell> GetNeighborsInMaze(MazeData data, MazeCell cell, bool[,] inMaze)
    {
        List<MazeCell> result = new List<MazeCell>();

        int[][] dirs = new int[][]
        {
            new int[]{ 0, 1 },   // 上
            new int[]{ 1, 0 },   // 右
            new int[]{ 0, -1 },  // 下
            new int[]{ -1, 0 }   // 左
        };

        foreach (var d in dirs)
        {
            int nx = cell.x + d[0];
            int ny = cell.y + d[1];
            MazeCell n = data.GetCell(nx, ny);
            if (n == null) continue;

            if (inMaze[nx, ny])
                result.Add(n);
        }

        return result;
    }

    /// <summary>
    /// 打通两格之间的墙（根据它们的坐标差）
    /// </summary>
    private static void RemoveWallBetween(MazeCell a, MazeCell b)
    {
        int dx = b.x - a.x;
        int dy = b.y - a.y;

        int dirFromA = -1;

        if (dx == 1 && dy == 0) dirFromA = 1;        // 右
        else if (dx == -1 && dy == 0) dirFromA = 3;  // 左
        else if (dx == 0 && dy == 1) dirFromA = 0;   // 上
        else if (dx == 0 && dy == -1) dirFromA = 2;  // 下

        if (dirFromA < 0) return;

        a.walls[dirFromA] = false;
        int opposite = (dirFromA + 2) % 4;
        b.walls[opposite] = false;
    }

    /// <summary>
    /// 用 BFS 在已经生成好的迷宫上，从 start 出发找“路径长度最远”的格子当终点。
    /// </summary>
    private static MazeCell FindFarthestCellByPathLength(MazeData data, MazeCell start)
    {
        int width = data.width;
        int height = data.height;

        int[,] dist = new int[width, height];
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                dist[x, y] = -1;
            }
        }

        Queue<MazeCell> q = new Queue<MazeCell>();
        dist[start.x, start.y] = 0;
        q.Enqueue(start);

        MazeCell farthest = start;

        while (q.Count > 0)
        {
            MazeCell c = q.Dequeue();
            int cd = dist[c.x, c.y];

            // 更新最远
            if (cd > dist[farthest.x, farthest.y])
                farthest = c;

            // 遍历已经打通的邻居
            foreach (MazeCell n in GetOpenNeighbors(data, c))
            {
                if (dist[n.x, n.y] < 0)
                {
                    dist[n.x, n.y] = cd + 1;
                    q.Enqueue(n);
                }
            }
        }

        return farthest;
    }

    /// <summary>
    /// 返回与当前格子之间“墙已打通”的邻居
    /// </summary>
    private static List<MazeCell> GetOpenNeighbors(MazeData data, MazeCell cell)
    {
        List<MazeCell> result = new List<MazeCell>();

        // 0 上, 1 右, 2 下, 3 左
        int[][] dirs = new int[][]
        {
            new int[]{ 0, 1 },
            new int[]{ 1, 0 },
            new int[]{ 0, -1 },
            new int[]{ -1, 0 }
        };

        for (int dir = 0; dir < 4; dir++)
        {
            if (!cell.walls[dir])
            {
                int nx = cell.x + dirs[dir][0];
                int ny = cell.y + dirs[dir][1];
                MazeCell n = data.GetCell(nx, ny);
                if (n != null)
                    result.Add(n);
            }
        }

        return result;
    }
}
